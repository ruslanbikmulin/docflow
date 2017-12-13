using Docflow.Configurations;
using Docflow.Container;
using Docflow.DtoEntities;
using Docflow.Interface;
using Docflow.Logging;

namespace Docflow.Controllers
{
    using Docflow.DAL.EntityContext;
    using Docflow.DAL.Models;
    using Docflow.Enums;
    using Docflow.Jobs;
    using Docflow.Scheduler;
    using Ext.Direct.Mvc;
    using OfficeOpenXml;
    using Quartz;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Web.Mvc;


    public class WorkController : DirectController
    {
        public WorkController()
        {
            this.DbContext = new DocumentContext();
        }

        private DocumentContext DbContext { get; set; }
        
        public ActionResult GetDirectoryList()
        {
            var result = new List<dynamic>();

            var tempId = 0;
            foreach (UserPathsElement userPathsElement in UserPaths.GetUserPaths())
            {
                if (!string.IsNullOrEmpty(userPathsElement.Path))
                {
                    result.Add(new
                    {
                        id = tempId++,
                        path = userPathsElement.Path
                    });
                }
            }
            
            return new JsonResult()
            {
                Data = result,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        
        [DirectIgnore]
        public ActionResult StartDownload()
        {
            var result = new
            {
                success = true,
                msg = "Успешное создание выгрузки."
            };
            
            if (string.IsNullOrEmpty(this.Request.Params["uploadLocation"]))
            {
                result = new
                {
                    success = false,
                    msg ="Запрет запуска выгрузки! Обратитесь к системному администратору."
                };
            }
            else
            {
                var uploadParams = new UploadParams()
                {
                    Email = "some@mail.ru",
                    UploadPath = this.Request.Params["uploadLocation"],
                    UploadStartDate = DateTime.Now,
                    UploadEndDate = DateTime.Now,
                    UploadStatus = UploadStatus.Starting,
                    ContractIdentifierFileStream = this.Request.InputStream
                };

                var uploadService = AppContainer.Container.Resolve<IUploadService>();
                uploadService.RegisterUpload(uploadParams);
            }

            return Json(result);
        }
        

        [DirectIgnore]
        public ActionResult CancelJob(string jobKey)
        {
            JobKey runningJobKey = JobKey.Create(jobKey.Split('.')[1], jobKey.Split('.')[0]);
            
            if (AppScheduler.LocalScheduler.Interrupt(runningJobKey))
            {
                
            }
            return new ContentResult()
            {
                Content = "Запустили прерывание процесса"
            };
        }

        public ActionResult GetUnloadStatus()
        {
            var result = new
            {
                isRunning = false,
                msg = "Выгрузка не запущена."
            };

            return Json(result);
        }

    }
}