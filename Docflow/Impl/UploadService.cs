using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
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

namespace Docflow.Impl
{
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

            var jobIdentity = string.Format("uploadJob_{0}", DateTime.Now.Ticks);
            var job = JobBuilder.Create<UnloadDocumentJob>().WithIdentity(jobIdentity, "UploadGroup").Build();
            job.JobDataMap["uploadID"] = uploadEntity.Id;

            // создаем крон триггер
            var trigger = TriggerBuilder.Create()
                .WithIdentity("UploadTriggerIdentity")
                // Задача будет запускаться каждый день
                // Задача будет исполняться с 01 часа ночи до 03 часов ночи
                // это означает что будет выделять процессорное время на выполнение кода в используемом jobe
                .WithCronSchedule("0 48-55 9 ? * *")
                .Build();

            AppScheduler.LocalScheduler.ScheduleJob(job, trigger);
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