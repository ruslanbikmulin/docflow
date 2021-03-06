﻿namespace Docflow.Jobs
{
    using Docflow.Container;
    using Docflow.DAL.EntityContext;
    using Docflow.DAL.Models;
    using Docflow.Enums;
    using Docflow.Interface;
    using Docflow.Logging;
    using Ionic.Zip;
    using OfficeOpenXml;
    using Quartz;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;

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

            Process currentProc = Process.GetCurrentProcess();
            

            AppLogging.Logger.Debug("Начали задачу");

            //this.DbContext.Database.Log = this.WriteSqlLog; 

            var upload = DbContext.UploadEntityTable.FirstOrDefault(u => u.UploadStatus < UploadStatus.Completed);

            if (upload == null)
            {
                // логировать и выходить
                var message = "Не удалось получить параметры активной выгрузки. Процесс прекращен.";
                AppLogging.Logger.Debug(message);
                
                context.Scheduler.UnscheduleJob(context.Trigger.Key);
                return;
            }

            AppLogging.Logger.Debug("Проверяем параметры на статус UploadStatus.Starting");

            // тут мы либо начинаем процесс либо возобновляем ранее приостановленный
            // если статус Starting то надо заполнить эту инфу
            if (upload.UploadStatus == UploadStatus.Starting)
            {
                // надо оценить количество свободного места
                string driveName = Path.GetPathRoot(upload.UploadPath);

                var driveInfo = new DriveInfo(driveName);

                var freeSpace = ((driveInfo.AvailableFreeSpace / 1024) / 1024) / 1024;

                if (freeSpace < 10)
                {
                    var message = "Выгрузка прервана, объем свободной памяти меньше 10 гб. Обратитесь к системному администратору!";
                    AppLogging.Logger.Debug(message);

                    context.Scheduler.UnscheduleJob(context.Trigger.Key);

                    this.SendEmail(upload.Email, "Выгрузка документов", message);
                    return;
                }

                var documentIdentifers = this.GetContractIdentifires(upload);
                if (documentIdentifers.Count == 0)
                {
                    var message = "Не удалось получить идентификаторы документов для выгрузки. Процесс прекращен.";
                    AppLogging.Logger.Debug(message);
                    upload.UploadStatus = UploadStatus.Canceled;
                    return;
                }

                foreach (var documentIdentifer in documentIdentifers)
                {
                    if (upload.UploadProgressRows.Any(di => di.ContractName == documentIdentifer))
                    {
                        AddError(upload.UploadPath, string.Format("Найден дубликат договора : {0}", documentIdentifer));
                        // нашли дубль
                        continue;
                    }

                    upload.UploadProgressRows.Add(new UploadProgress()
                    {
                        ContractName = documentIdentifer,
                        ProgressStatus = ProgressStatus.ToDo
                    });
                }

                // подготовку завершили, переходим к сбору информации о выгружаемых файлах
                AppLogging.Logger.Info("Смена статуса на FileInfoFilling");
                this.ChangeUploadStatus(upload, UploadStatus.FileInfoFilling);
            }
            
            if (upload.UploadStatus == UploadStatus.FileInfoFilling)
            {
                #region Сбор информации

                AppLogging.Logger.Debug("Сбор информации.");
                try
                {
                    // надо прежде проверить подключение к базе для получения метаинформации
                    if (this.MetaConnectionIsFine())
                    {
                        var stopwatch = Stopwatch.StartNew();
                        foreach (var uploadProgress in upload.UploadProgressRows.Where(up => up.ProgressStatus == ProgressStatus.ToDo))
                        {
                            // если вышли за пределы рабочего временного промежутка
                            if (TimePartIsOver(context.FireTimeUtc.Value.DateTime.ToLocalTime()))
                            {
                                return;
                            }

                            // если выгрузку прервали без возможности возобновления
                            if (this.InterruptRequest)
                            {
                                ChangeUploadStatus(upload, UploadStatus.Canceled);
                                context.Scheduler.UnscheduleJob(context.Trigger.Key);
                                return;
                            }

                            // для каждого договора работаем в рамках одного подключения
                            using (var fileInfoFiller = AppContainer.Container.Resolve<IFileInfoFiller>())
                            {
                                stopwatch.Start();

                                //uploadProgress.ScanPaths = fileInfoFiller.FillFileInfo(uploadProgress.ContractName);
                                var result = fileInfoFiller.FillFileInfo(uploadProgress.ContractName);

                                if (!result.Success)
                                {
                                    uploadProgress.ProgressStatus = ProgressStatus.Error;
                                    
                                    AppLogging.Logger.Debug(string.Format("Не удалось получить инфу о путях по договору {0}", uploadProgress.ContractName));
                                }
                                else
                                {
                                    // пути до файлов получены
                                    AppLogging.Logger.Debug("Найдены пути по файлам для договора:" + uploadProgress.ContractName);
                                    uploadProgress.ScanPaths = result.ScanPaths;
                                    uploadProgress.ProgressStatus = ProgressStatus.PathFound;
                                }

                                stopwatch.Stop();

                                AppLogging.Logger.Debug("Получение метаинформации по договору: " + uploadProgress.ContractName + " заняло времени: " + stopwatch.Elapsed);
                                stopwatch.Reset();
                                this.SaveProgress(uploadProgress);

                            }
                        }

                        AppLogging.Logger.Info("Смена статуса на Uploading");
                        this.ChangeUploadStatus(upload, UploadStatus.Uploading);
                    }
                }
                catch (Exception e)
                {
                    // ошибка, вероятно проблемы с подключением
                    if (e.InnerException != null)
                    {
                        AppLogging.Logger.Debug("Проблемы на этапе получения метаинформации о файлах" + e.Message + e.StackTrace + e.InnerException.Message);
                    }
                    else
                    {
                        AppLogging.Logger.Debug("Проблемы на этапе получения метаинформации о файлах" + e.Message + e.StackTrace);
                    }

                    
                    ChangeUploadStatus(upload, UploadStatus.Canceled);
                    context.Scheduler.UnscheduleJob(context.Trigger.Key);
                }
                #endregion
            }

            if (upload.UploadStatus == UploadStatus.Uploading)
            {
                #region Выгрузка
                try
                {
                    var rowsForUpload = upload.UploadProgressRows.Where(ur =>
                        ur.ScanPaths.Any(s => s.ScanStatus == ScanStatus.FilePathAssigned));

                    var forUploadRowsList = rowsForUpload as IList<UploadProgress> ?? rowsForUpload.ToList();

                    if (!forUploadRowsList.Any())
                    {
                        AppLogging.Logger.Debug("На этапе выгрузки нет договоров для выгрузки");
                        this.ChangeUploadStatus(upload, UploadStatus.Finishing);
                    }
                    else
                    {
                        // ВЫГРУЗКА
                        // обработать только те договоры по которым узнали пути до файлов
                        foreach (var uploadUploadProgressRow in forUploadRowsList)
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

                            var spNotDmlIds = new List<long>();

                            // отбираем те файлы которые имеют установленный путь и не требуют создания внутренней директории ДМЛ
                            foreach (var scanPath in uploadUploadProgressRow.ScanPaths.Where(s => s.ScanStatus == ScanStatus.FilePathAssigned && !s.NeedPathForDml))
                            {

                                spNotDmlIds.Add(scanPath.Id);
                            }

                            foreach (var scanPathId in spNotDmlIds)
                            {
                                var scanPath = this.DbContext.ScanPaths.FirstOrDefault(s => s.Id == scanPathId);
                                if (scanPath == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    var fileBytes = File.ReadAllBytes(scanPath.FilePath);

                                    var numberContractForPathName = uploadUploadProgressRow.ContractName.Replace("/", ".").Replace("*", ".").Replace("\\", ".");

                                    if (!Directory.Exists(Path.Combine(upload.UploadPath, numberContractForPathName)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(upload.UploadPath, numberContractForPathName));
                                    }


                                    scanPath.ResultFileName = Path.Combine(upload.UploadPath, numberContractForPathName, scanPath.FileName);
                                    using (var fs = new FileStream(scanPath.ResultFileName, FileMode.Create))
                                    {
                                        fs.Write(fileBytes, 0, fileBytes.Length);
                                    }

                                    scanPath.ScanStatus = ScanStatus.Downloaded;
                                    
                                }
                                catch (Exception e)
                                {
                                    if (e.InnerException == null)
                                    {
                                        AppLogging.Logger.Debug("Не удалось скопировать файл скана:" + e.Message + e.StackTrace);
                                    }
                                    else
                                    {
                                        AppLogging.Logger.Debug("Не удалось скопировать файл скана:" + e.Message + e.StackTrace + e.InnerException.Message);
                                    }
                                }
                            }

                            this.SaveProgress(uploadUploadProgressRow);

                            var spDmlIds = new List<long>();

                            foreach (var scanPath in uploadUploadProgressRow.ScanPaths.Where(s => s.ScanStatus == ScanStatus.FilePathAssigned && s.NeedPathForDml))
                            {
                                spDmlIds.Add(scanPath.Id);
                            }

                            foreach (var scanPathId in spDmlIds)
                            {
                                var scanPath = this.DbContext.ScanPaths.FirstOrDefault(s => s.Id == scanPathId);
                                if (scanPath == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    var fileBytes = File.ReadAllBytes(scanPath.FilePath);

                                    var numberContractForPathName = uploadUploadProgressRow.ContractName.Replace("/", ".").Replace("*", ".").Replace("\\", ".");

                                    if (!Directory.Exists(Path.Combine(upload.UploadPath, numberContractForPathName, "dml")))
                                    {
                                        Directory.CreateDirectory(Path.Combine(upload.UploadPath, numberContractForPathName, "dml"));
                                    }

                                    scanPath.ResultFileName = Path.Combine(upload.UploadPath, numberContractForPathName, "dml", scanPath.FileName);
                                    using (var fs = new FileStream(scanPath.ResultFileName, FileMode.Create))
                                    {
                                        fs.Write(fileBytes, 0, fileBytes.Length);
                                    }

                                    scanPath.ScanStatus = ScanStatus.Downloaded;
                                    
                                }
                                catch (Exception e)
                                {
                                    if (e.InnerException == null)
                                    {
                                        AppLogging.Logger.Debug("Не удалось скопировать файл скана:" + e.Message + e.StackTrace);
                                    }
                                    else
                                    {
                                        AppLogging.Logger.Debug("Не удалось скопировать файл скана:" + e.Message + e.StackTrace + e.InnerException.Message);
                                    }
                                }
                            }

                            uploadUploadProgressRow.ProgressStatus = ProgressStatus.Uploaded;
                            this.SaveProgress(uploadUploadProgressRow);

                        }
                    }

                    AppLogging.Logger.Info("Смена статуса на Zipping");
                    this.ChangeUploadStatus(upload, UploadStatus.Zipping);
                }
                catch (Exception e)
                {
                    AppLogging.Logger.Fatal(e, "Не удалось активировать модуль получения файлов. Вероятные причины: нет удалось подключиться к базе данных." + e.Message + e.StackTrace);
                    try
                    {
                        this.ChangeUploadStatus(upload, UploadStatus.Canceled);
                    }
                    catch (Exception exception)
                    {
                        AppLogging.Logger.Fatal("Не удается  удалить регистрацию выгрузки:" + exception.Message + e.StackTrace);
                    }

                    context.Scheduler.UnscheduleJob(context.Trigger.Key);
                    return;
                }
                #endregion
            }

            if (upload.UploadStatus == UploadStatus.Zipping)
            {
                var rowsForZipping =
                    upload.UploadProgressRows.Where(ur => ur.ScanPaths.Any(s => s.ScanStatus == ScanStatus.Downloaded));

                if (!rowsForZipping.Any())
                {
                    AppLogging.Logger.Debug("На этапе архивации нет договоров для архивации");
                    this.ChangeUploadStatus(upload, UploadStatus.Finishing);
                }
                else
                {
                    #region  Архивация
                    try
                    {
                        var contractCountInArchive = double.Parse(ConfigurationManager.AppSettings["countToArchive"]);

                        var archiveIndex = Math.Round(upload.UploadProgressRows.Count(progress => progress.ProgressStatus == ProgressStatus.Zipped) / contractCountInArchive, MidpointRounding.AwayFromZero);

                        if (archiveIndex < 1)
                        {
                            archiveIndex = 1;
                        }

                        var archivePath = Path.Combine(upload.UploadPath, string.Format("result{0}.zip", archiveIndex));

                        using (var zipFile = File.Exists(archivePath) ? ZipFile.Read(archivePath) : new ZipFile(archivePath))
                        {
                            zipFile.AlternateEncoding = Encoding.GetEncoding("cp866");
                            zipFile.AlternateEncodingUsage = ZipOption.Always;
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
                                    return;
                                }

                                var freshArchiveIndex = Math.Round(upload.UploadProgressRows.Count(progress => progress.ProgressStatus == ProgressStatus.Zipped) / contractCountInArchive, MidpointRounding.AwayFromZero);

                                if (freshArchiveIndex > archiveIndex)
                                {
                                    // выходим чтобы создать новый архив
                                    return;
                                }

                                try
                                {
                                    foreach (var scanPath in uploadUploadProgressRow.ScanPaths.Where(s => s.ScanStatus == ScanStatus.Downloaded))
                                    {
                                        try
                                        {
                                            zipFile.AddFile(scanPath.ResultFileName);
                                            AppLogging.Logger.Debug("Добавили файл в архив");
                                            // убираем сохранение на каждый файл
                                            // zipFile.Save();
                                            AppLogging.Logger.Debug("Сохранили файл в архиве. " + scanPath.FileName);
                                            scanPath.ScanStatus = ScanStatus.Zipped;
                                        }
                                        catch (Exception e)
                                        {
                                            var message = string.Format("Ошибка при добавлении файла документа в архив. Файл: {0}", scanPath.ResultFileName);
                                            AppLogging.Logger.Debug(message);
                                            scanPath.ScanStatus = ScanStatus.Error;
                                        }
                                    }


                                    zipFile.Save();

                                    uploadUploadProgressRow.ProgressStatus = ProgressStatus.Zipped;
                                    SaveProgress(uploadUploadProgressRow);
                                }
                                catch (Exception e)
                                {
                                    var message = string.Format("Ошибка при архивации файлов договора . Номер договора: {0}, Описание: {1}", uploadUploadProgressRow.ContractName, e.Message);
                                    AppLogging.Logger.Debug(message);
                                    uploadUploadProgressRow.ProgressStatus = ProgressStatus.Error;
                                    SaveProgress(uploadUploadProgressRow);
                                }
                            }
                        }
                        AppLogging.Logger.Info("Смена статуса на Finishing");
                        this.ChangeUploadStatus(upload, UploadStatus.Finishing);
                    }
                    catch (Exception e)
                    {
                        AppLogging.Logger.Debug("Ошибка при создании или изменении архива.");
                        ChangeUploadStatus(upload, UploadStatus.Canceled);
                        context.Scheduler.UnscheduleJob(context.Trigger.Key);
                        return;
                    }
                    #endregion

                }
            }


            AppLogging.Logger.Debug("Объем памяти: " + currentProc.PagedMemorySize64);
                

            #region Если нужно удалять файлы
            //foreach (var uploadUploadProgressRow in upload.UploadProgressRows)
            //{
            //    try
            //    {
            //        if (File.Exists(uploadUploadProgressRow.FullFilePath))
            //        {
            //            File.Delete(uploadUploadProgressRow.FullFilePath);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        AppLogging.Logger.Error(e ,"Не удалось удалить файл");
            //    }
            //}
            #endregion

            if (upload.UploadStatus == UploadStatus.Finishing)
            {
                upload.UploadEndDate = DateTime.Now.ToLocalTime();

                AppLogging.Logger.Info("Формируем протокол выполненной работы.");

                var errorsExist = upload.UploadProgressRows
                    .Where(up => up.ScanPaths
                        .Any(s =>
                            s.ScanStatus == ScanStatus.Error 
                        || s.ScanStatus == ScanStatus.FileNotExist 
                        || s.ScanStatus == ScanStatus.NotDownloaded));

                var errorsUploadProgressRows = errorsExist as UploadProgress[] ?? errorsExist.ToArray();
                if (errorsUploadProgressRows.Any())
                {
                    using (var streamWriter = new StreamWriter(upload.UploadPath + "\\error.txt"))
                    {
                        foreach (var uploadUploadProgressRow in errorsUploadProgressRows)
                        {
                            streamWriter.WriteLine("Ошибки по договору:" + uploadUploadProgressRow.ContractName);
                            streamWriter.WriteLine(string.Format("Количество файлов с ошибкой: {0}", uploadUploadProgressRow.ScanPaths.Count(path => path.ScanStatus == ScanStatus.Error)));
                            streamWriter.WriteLine(string.Format("Количество не найденных файлов: {0}", uploadUploadProgressRow.ScanPaths.Count(path => path.ScanStatus == ScanStatus.FileNotExist)));
                            streamWriter.WriteLine("---------------------------------------------------------------------------------------------------");
                        }
                    }
                }

                var successUploads = upload.UploadProgressRows
                    .Where(ur => ur.ScanPaths.Any(s => s.ScanStatus == ScanStatus.Zipped));

                var successRows = successUploads as UploadProgress[] ?? successUploads.ToArray();
                if (successRows.Any())
                {
                    using (var streamWriter = new StreamWriter(upload.UploadPath + "\\success.txt"))
                    {
                        streamWriter.WriteLine(string.Format("Общее количество договоров: {0}", upload.ContractCount));
                        streamWriter.WriteLine(string.Format("Количество заархивированных: {0}", upload.ZippedCount));
                        streamWriter.WriteLine(string.Format("Количество договоров с ошибками: {0}", errorsUploadProgressRows.Length));
                        streamWriter.WriteLine(string.Format("Начало выгрузки: {0}", upload.UploadStartDate));
                        streamWriter.WriteLine(string.Format("Конец выгрузки: {0}", upload.UploadEndDate));

                        streamWriter.WriteLine("************************************************************");
                        streamWriter.WriteLine("Детализация по успешно выгруженным договорам");
                        streamWriter.WriteLine("************************************************************");

                        foreach (var uploadUploadProgressRow in successRows)
                        {
                            streamWriter.WriteLine(string.Format("Обработан договор: {0}", uploadUploadProgressRow.ContractName));


                            if (uploadUploadProgressRow.ScanPaths.Any(s => s.NeedPathForDml))
                            {
                                streamWriter.WriteLine(string.Format("Общее количество файлов договора-транша: {0}", uploadUploadProgressRow.ScanPaths.Count(s => !s.NeedPathForDml)));
                                streamWriter.WriteLine(string.Format("Общее количество файлов  договора-мз: {0}", uploadUploadProgressRow.ScanPaths.Count(s => s.NeedPathForDml)));
                            }
                            else
                            {
                                streamWriter.WriteLine(string.Format("Общее количество файлов договора: {0}", uploadUploadProgressRow.ScanPaths.Count));
                            }

                            streamWriter.WriteLine(string.Format("Количество заархивированных файлов: {0}", uploadUploadProgressRow.ScanPaths.Count(path => path.ScanStatus == ScanStatus.Zipped)));
                            streamWriter.WriteLine(string.Format("Количество файлов с ошибкой: {0}", uploadUploadProgressRow.ScanPaths.Count(path => path.ScanStatus == ScanStatus.Error)));
                            streamWriter.WriteLine(string.Format("Количество не найденных файлов: {0}", uploadUploadProgressRow.ScanPaths.Count(path => path.ScanStatus == ScanStatus.FileNotExist)));
                        }
                    }

                    AppLogging.Logger.Info("Закончилась выгрузка. Файл архива: " + upload.UploadPath + "\\result.zip");
                }
                
                
                AppLogging.Logger.Debug("Попытка отправить письмо по указанным в конфиге параметрам");

                try
                {
                    if (!successRows.Any())
                    {
                        this.SendEmail(upload.Email, "Выгрузка документов", "Выгрузка завершилась неуспешно, обратитесь к системному администратору!");
                    }
                    else
                    {
                        this.SendEmail(upload.Email, "Выгрузка документов", "Закончилась выгрузка, файлы размещены в папке: " + upload.UploadPath);
                    }

                    
                }
                catch (Exception e)
                {
                    AppLogging.Logger.Debug("Ошибка при попытке отправить письмо по указанным параметрам:" + upload.Email + ". Описание: " + e.Message);
                }
                
                // убираем задачу из планировщика
                AppLogging.Logger.Info("Смена статуса на Completed");
                this.ChangeUploadStatus(upload, UploadStatus.Completed);
                context.Scheduler.UnscheduleJob(context.Trigger.Key);
            }
        }

        /// <summary>
        ///  Метод определяет выполнение работы частями времени опционально.
        ///  потому как планировщик не хранит информацию о том что время интервала закончилось
        /// </summary>
        /// <param name="fireTime">Время срабатывания триггера</param>
        /// <returns></returns>
        private bool TimePartIsOver(DateTime fireTime)
        {
            var timePart = fireTime.Add(new TimeSpan(0, 0, 120, 0));

            bool result = (DateTime.Compare(timePart, DateTime.Now) < 0);

            return result;
        }
        
        public void Interrupt()
        {
            this.InterruptRequest = true;
        }

        public void SaveScanPath(ScanPath scanPath)
        {
            this.DbContext.ScanPaths.Attach(scanPath);
            this.DbContext.Entry(scanPath).State = EntityState.Modified;
            this.DbContext.SaveChanges();
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
        public List<string> GetContractIdentifires(UploadEntity uploadEntity)
        {
            var result = new List<string>();
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
                            for (var rowNum = 1; rowNum < 100000; rowNum++)
                            {
                                var cell = worksheet.Cells[rowNum, 1];

                                if (cell.Value != null
                                    && !string.IsNullOrEmpty(cell.Value.ToString()))
                                {
                                    result.Add(cell.Value.ToString().Trim());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppLogging.Logger.Debug("Ошибка при чтении файла идентификаторов.");
            }
            
            return result;
        }

        /// <summary>
        /// Чекаем подключение
        /// </summary>
        /// <returns></returns>
        private bool MetaConnectionIsFine()
        {
            try
            {
                SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["FOConnection"].ConnectionString);
                connection.Open();
                connection.Close();
            }
            catch (Exception e)
            {
                AppLogging.Logger.Debug(e);
                return false;
            }

            return true;
        }

        public void SendEmail(string to, string subject, string message)
        {
            var mailMessage = new MailMessage();

            mailMessage.From = new MailAddress(ConfigurationManager.AppSettings["emailFrom"]);
            mailMessage.To.Add(new MailAddress(to));
            mailMessage.Subject = subject;
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;

            var gmailSmtpClient = new SmtpClient();
            gmailSmtpClient.Host = ConfigurationManager.AppSettings["emailHost"];
            gmailSmtpClient.Port =  Int32.Parse(ConfigurationManager.AppSettings["emailPort"]);
            gmailSmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            gmailSmtpClient.Send(mailMessage);
        }

        private void AddError(string path,string message)
        {
            using (var streamWriter = new StreamWriter(path + "\\errors.txt", true))
            {
                streamWriter.WriteLine(message);
            }
        }

        public void WriteSqlLog(string query)
        {
            AppLogging.Logger.Info(query);
        }
    }
}