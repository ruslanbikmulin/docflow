namespace Docflow.Jobs
{
    using Docflow.DAL.EntityContext;
    using Docflow.DAL.Models;
    using Docflow.Enums;
    using Docflow.Logging;
    using Ionic.Zip;
    using OfficeOpenXml;
    using Quartz;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;

    /// <summary>
    /// Класс выгрузки документов из бд
    /// </summary>
    public class UnloadDocumentJob : IInterruptableJob
    {
        public UnloadDocumentJob()
        {
            this.DbContext = new DocumentContext();
        }

        private DocumentContext DbContext { get; set; }

        public int UploadId { get; set; }

        private bool InterruptRequest { get; set; }

        public void Execute(IJobExecutionContext context)
        {
            var upload = DbContext.UploadEntityTable.FirstOrDefault(u => u.UploadStatus < UploadStatus.Completed);

            if (upload == null)
            {
                // логировать и выходить
                var message = "Не удалось получить параметры активной выгрузки. Процесс прекращен.";
                AppLogging.Logger.Error(message);
                
                context.Scheduler.UnscheduleJob(context.Trigger.Key);
                return;
            }
            
            // тут мы либо начинаем процесс либо возобновляем ранее приостановленный
            // если статус Starting то надо заполнить эту инфу
            if (upload.UploadStatus == UploadStatus.Starting)
            {
                var documentIdentifers = this.GetContractIdentifires(upload);
                if (documentIdentifers.Count == 0)
                {
                    var message = "Не удалось получить идентификаторы документов для выгрузки. Процесс прекращен.";
                    AppLogging.Logger.Error(message);
                    upload.UploadStatus = UploadStatus.Canceled;
                    return;
                }

                foreach (var documentIdentifer in documentIdentifers)
                {
                    upload.UploadProgressRows.Add(new UploadProgress()
                    {
                        ContractName = documentIdentifer,
                        ProgressStatus = ProgressStatus.ToDo
                    });
                }

                // подготовку завершили, переходим к выгрузке
                this.ChangeUploadStatus(upload, UploadStatus.Uploading);
            }
            
            // ВЫГРУЗКА
            // обработать документы со статусом ProgressStatus.ToDo которые ещё не выгружались
            foreach (var uploadUploadProgressRow in upload.UploadProgressRows.Where(up => up.ProgressStatus == ProgressStatus.ToDo))
            {
                // перед тем как обработать очередной документ узнаем про отмену
                if (TimePartIsOver(context.FireTimeUtc.Value.DateTime.ToLocalTime()))
                {
                    return;
                }

                if (this.InterruptRequest)
                {
                    ChangeUploadStatus(upload, UploadStatus.Canceled);
                    context.Scheduler.UnscheduleJob(context.Trigger.Key);
                }
                
                try
                {
                    var fileBytes = GetFileFromNetwork(uploadUploadProgressRow);
                    if (uploadUploadProgressRow.ErrorCode != ErrorCode.None)
                    {
                        // произошло что-то неладное
                        // сохраняем прогресс по файлу
                        // переходим к следующему файлу
                        SaveProgress(uploadUploadProgressRow);
                        continue;
                    }

                    if (fileBytes != null)
                    {
                        // записали в папку из настроек выгрузки
                        // todo допилить момент по определению расширения и тп.
                        var fileToSaveFromConfig =
                            string.Format("{0}file_{2}{1}", upload.UploadPath, ".pdf", DateTime.Now.Ticks);

                        using (var fileStream = new FileStream(fileToSaveFromConfig, FileMode.Create, FileAccess.Write))
                        {
                            fileStream.Write(fileBytes, 0, fileBytes.Length);
                        }

                        uploadUploadProgressRow.FullFilePath = fileToSaveFromConfig;
                        uploadUploadProgressRow.ProgressStatus = ProgressStatus.Uploaded;

                    }
                    else
                    {
                        uploadUploadProgressRow.ProgressStatus = ProgressStatus.Error;
                        uploadUploadProgressRow.ErrorCode = ErrorCode.NetworkError;
                    }


                    // выгрузили надо сменить статус по документу
                    SaveProgress(uploadUploadProgressRow);
                }
                catch (FileNotFoundException e)
                {
                    uploadUploadProgressRow.ProgressStatus = ProgressStatus.Error;
                    uploadUploadProgressRow.ErrorCode = ErrorCode.ScanFileNotFound;
                    var message = string.Format("Ошибка при копировании файла документа . Файл: {0}", uploadUploadProgressRow.FullFilePath);
                    AppLogging.Logger.Error(e, message);
                }
                catch (Exception e)
                {
                    uploadUploadProgressRow.ProgressStatus = ProgressStatus.Error;
                    uploadUploadProgressRow.ErrorCode = ErrorCode.NetworkError;
                    var message = string.Format("Ошибка при копировании файла документа . Файл: {0}", uploadUploadProgressRow.FullFilePath);
                    AppLogging.Logger.Error(e, message);
                }
                
            }
            
            // АРХИВАЦИЯ
            this.ChangeUploadStatus(upload, UploadStatus.Zipping);
            try
            {
                using (var zipFile = new ZipFile(upload.UploadPath + "result.zip"))
                {
                    // обработать документы которые уже выгрузили на диск со статусом Uploaded
                    foreach (var uploadUploadProgressRow in upload.UploadProgressRows.Where(up => up.ProgressStatus == ProgressStatus.Uploaded))
                    {
                        if (TimePartIsOver(context.FireTimeUtc.Value.DateTime.ToLocalTime()))
                        {
                            // как то зафиксировать этот момент
                            return;
                        }

                        // перед тем как обработать очередной документ узнаем про отмену
                        if (this.InterruptRequest)
                        {
                            ChangeUploadStatus(upload, UploadStatus.Canceled);
                            context.Scheduler.UnscheduleJob(context.Trigger.Key);
                        }

                        try
                        {
                            using (var fileStream = new FileStream(uploadUploadProgressRow.FullFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                            {
                                var content = new byte[fileStream.Length];
                                fileStream.Read(content, 0, Convert.ToInt32(fileStream.Length));

                                zipFile.AddEntry(Path.GetFileName(uploadUploadProgressRow.FullFilePath), content);

                                
                                zipFile.Save();
                                uploadUploadProgressRow.ProgressStatus = ProgressStatus.Zipped;
                                
                            }
                        }
                        catch (Exception e)
                        {
                            var message = string.Format("Ошибка при добавлении файла документа в архив. Файл: {0}", uploadUploadProgressRow.FullFilePath);
                            AppLogging.Logger.Error(e, message);
                            uploadUploadProgressRow.ProgressStatus = ProgressStatus.Error;
                            SaveProgress(uploadUploadProgressRow);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppLogging.Logger.Error(e, "Ошибка при создании или изменении архива.");
            }
            
            this.ChangeUploadStatus(upload, UploadStatus.Finishing);

            foreach (var uploadUploadProgressRow in upload.UploadProgressRows)
            {
                try
                {
                    if (File.Exists(uploadUploadProgressRow.FullFilePath))
                    {
                        File.Delete(uploadUploadProgressRow.FullFilePath);
                    }
                }
                catch (Exception e)
                {
                    AppLogging.Logger.Error(e ,"Не удалось удалить файл");
                }
            }

            // какие то завершающие шаги
            // меняем статус на завершенный
            this.ChangeUploadStatus(upload, UploadStatus.Completed);

            AppLogging.Logger.Info("Закончилась выгрузка. Файл архива: " + upload.UploadPath + "\\result.zip");

            this.SendEmail("gomerSimpson@gmail.com", "Выгрузка документов", "Закончилась выгрузка");

            // убираем задачу из планировщика
            context.Scheduler.UnscheduleJob(context.Trigger.Key);
        }

        /// <summary>
        ///  Метод определяет выполнение работы частями времени опционально.
        ///  потому как планировщик не хранит информацию о том что время интервала закончилось
        /// </summary>
        /// <param name="fireTime">Время срабатывания триггера</param>
        /// <returns></returns>
        private bool TimePartIsOver(DateTime fireTime)
        {
            var timePart = fireTime.Add(new TimeSpan(0, 0, 2, 0));

            bool result = (DateTime.Compare(timePart, DateTime.Now) < 0);

            return result;
        }

        private byte[] GetFileFromNetwork(UploadProgress uploadProgressRow)
        {

            try
            {

                // если проблемы с сетью
                uploadProgressRow.ErrorCode = ErrorCode.NetworkError;

                // если проблемы с доступом к джой казино
                uploadProgressRow.ErrorCode = ErrorCode.FileSystemAccessDisable;

                // если не нашли папку
                uploadProgressRow.ErrorCode = ErrorCode.PartOfScansNotFound;

                // если не нашли файл
                uploadProgressRow.ErrorCode = ErrorCode.ScanFileNotFound;
            }
            catch (Exception e)
            {

            }
            

            return null;
        }

        public void Interrupt()
        {
            this.InterruptRequest = true;
        }

        public void SaveProgress(UploadProgress uploadProgress)
        {
            this.DbContext.UploadProgressTable.Attach(uploadProgress);
            this.DbContext.Entry(uploadProgress).State = EntityState.Modified;
            this.DbContext.SaveChanges();
        }

        public void ChangeUploadStatus(UploadEntity uploadEntity, UploadStatus uploadStatus)
        {
            uploadEntity.UploadStatus = uploadStatus;

            this.DbContext.UploadEntityTable.Attach(uploadEntity);
            this.DbContext.Entry(uploadEntity).State = EntityState.Modified;
            this.DbContext.SaveChanges();
        }

        /// <summary>
        /// получаем идентификаторы договоров из входящего документа
        /// </summary>
        /// <param name="uploadEntity"></param>
        /// <returns></returns>
        public List<int> GetContractIdentifires(UploadEntity uploadEntity)
        {
            var result = new List<int>();
            try
            {
                if (File.Exists(uploadEntity.FilePath))
                {
                    using (var fileStream = new FileStream(uploadEntity.FilePath, FileMode.Open))
                    {
                        using (var package = new ExcelPackage(fileStream))
                        {
                            var workbook = package.Workbook;
                            var worksheet = workbook.Worksheets.First();

                            // до каких пор читаем строки в файле? пока ставим 10000
                            for (var rowNum = 1; rowNum < 10000; rowNum++)
                            {

                                var cell = worksheet.Cells[rowNum, 1];
                                var cellValue = 0;

                                if (cell.Value != null
                                    && !string.IsNullOrEmpty(cell.Value.ToString())
                                    && int.TryParse(cell.Value.ToString(), out cellValue))
                                {
                                    result.Add(cellValue);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppLogging.Logger.Error(e, "Ошибка при чтении файла идентификаторов.");
            }
            
            return result;
        }



        public void SendEmail(string to, string subject, string message)
        {
            MailMessage mailMessage = new MailMessage();

            mailMessage.From = new MailAddress("puretimefinder@gmail.com");
            mailMessage.To.Add(new MailAddress(to));

            mailMessage.Subject = subject;
            mailMessage.Body = message;

            mailMessage.IsBodyHtml = true;

            SmtpClient gmailSmtpClient = new SmtpClient();
            gmailSmtpClient.Host = "smtp.gmail.com";
            gmailSmtpClient.Port = 465;
            gmailSmtpClient.EnableSsl = true;
            gmailSmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            gmailSmtpClient.Credentials = new System.Net.NetworkCredential("puretimefinder@gmail.com", "eV5XYAw5vXAT");

            gmailSmtpClient.Send(mailMessage);
        }
        
    }
}