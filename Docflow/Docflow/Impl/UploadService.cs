namespace Docflow.Impl
{
    using Docflow.DAL.EntityContext;
    using Docflow.DAL.Models;
    using Docflow.DtoEntities;
    using Docflow.Enums;
    using Docflow.Interface;
    using Docflow.Jobs;
    using Docflow.Logging;
    using Docflow.Scheduler;
    using OfficeOpenXml;
    using Quartz;
    using System;
    using System.Configuration;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;

    public class UploadService : IUploadService
    {
        public UploadService()
        {
            this.DbContext = new DocumentContext();
        }

        public DocumentContext DbContext { get; set; }

        public void RegisterUpload(UploadParams uploadParams)
        {
            var contractListfilePath = this.SaveContractList(uploadParams.ContractIdentifierFileStream);

            if (string.IsNullOrEmpty(contractListfilePath))
            {
                AppLogging.Logger.Error("Не удалось получить файл с идентификаторами.");
                return;
            }

            AppLogging.Logger.Debug("Сохранили файл с номерами договоров.");

            var uploadEntity = new UploadEntity()
            {
                Email = uploadParams.Email,
                FilePath = contractListfilePath,
                UploadPath = uploadParams.UploadPath,
                UploadStartDate = uploadParams.UploadStartDate,
                UploadEndDate = DateTime.Now,
                UploadStatus = UploadStatus.Starting
            };

            this.DbContext.UploadEntityTable.Add(uploadEntity);
            this.DbContext.SaveChanges();
            
            var job = JobBuilder.Create<UnloadDocumentJob>().WithIdentity("worker").Build();

            AppLogging.Logger.Debug("Создаем задачу в планировщике. Параметры: " + ConfigurationManager.AppSettings["SheduleSettings"]);
            // создаем крон триггер
            var trigger = TriggerBuilder.Create()
                .WithIdentity("UploadTriggerIdentity")
                // Задача будет запускаться каждый день
                // Задача будет исполняться с 01 часа ночи до 03 часов ночи
                // это означает что будет выделять процессорное время на выполнение кода в используемом jobe
                .WithCronSchedule(ConfigurationManager.AppSettings["SheduleSettings"]) 
                //.StartNow()
                .Build();
            AppLogging.Logger.Debug("Создали задачу в планировщике.");

            AppScheduler.LocalScheduler.ScheduleJob(job, trigger);
            AppLogging.Logger.Debug("Зарегистрировали задачу.");
        }

        public void UnregistrateRunningUpload()
        {
            var upload = this.GetRunningUpload();
            if (upload != null)
            {
                upload.UploadStatus = UploadStatus.Canceled;

                this.DbContext.UploadEntityTable.Attach(upload);
                this.DbContext.Entry(upload).State = EntityState.Modified;
                this.DbContext.SaveChanges();
            }
        }


        public UploadEntity GetRunningUpload()
        {
            var upload = this.DbContext.UploadEntityTable.FirstOrDefault(u =>
                u.UploadStatus < UploadStatus.Completed);

            return upload;
        }

        private string SaveContractList(Stream stream)
        {
            var fileFullPath = string.Empty;
            if (stream != null)
            {
                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var contractIdentifiresSavePath = ConfigurationManager.AppSettings["ContractIdentifiresSavePath"];
                        if (string.IsNullOrEmpty(contractIdentifiresSavePath))
                        {
                            throw new Exception("Не указан каталог для сохранения!");
                        }

                        fileFullPath = string.Format("{0}{1}", contractIdentifiresSavePath, "contracts.xlsx");
                        package.SaveAs(new FileInfo(fileFullPath));
                    }
                }
                catch (Exception e)
                {
                    AppLogging.Logger.Error(e, "Не удалось обработать файл идентификаторов документов. Задача прекращена.");
                }
            }

            return fileFullPath;
        }
    }
}