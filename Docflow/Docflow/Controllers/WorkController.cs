namespace Docflow.Controllers
{
    using Docflow.Configurations;
    using Docflow.Container;
    using Docflow.DAL.EntityContext;
    using Docflow.DtoEntities;
    using Docflow.Enums;
    using Docflow.Interface;
    using Docflow.Scheduler;
    using Ext.Direct.Mvc;
    using Quartz;
    using System;
    using System.Collections.Generic;
    using System.Web.Mvc;


    public class WorkController : DirectController
    {
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
        public JsonResult StartDownload()
        {
            if (!string.IsNullOrEmpty(this.Request.Params["uploadLocation"]))
            {
                var uploadParams = new UploadParams()
                {
                    Email = this.Request.Params["userEmail"],
                    UploadPath = this.Request.Params["uploadLocation"],
                    UploadStartDate = DateTime.Now,
                    UploadEndDate = DateTime.Now,
                    UploadStatus = UploadStatus.Starting,
                    ContractIdentifierFileStream = this.Request.InputStream
                };

                var uploadService = AppContainer.Container.Resolve<IUploadService>();
                uploadService.RegisterUpload(uploadParams);
            }

            return Json(new
            {
                success = true
            });
        }

        public ActionResult CancelJob()
        {
            var uploadService = AppContainer.Container.Resolve<IUploadService>();
            uploadService.UnregistrateRunningUpload();

            JobKey runningJobKey = JobKey.Create("worker");
            AppScheduler.LocalScheduler.Interrupt(runningJobKey);

            return new ContentResult()
            {
                Content = "Запустили прерывание процесса"
            };
        }

        public ActionResult GetUnloadStatus()
        {
            var uploadService = AppContainer.Container.Resolve<IUploadService>();
            var upload = uploadService.GetRunningUpload();
            if (upload != null)
            {
                return Json(new
                {
                    isRunning = true,
                    msg = "Выгрузка зарегистрирована."
                });
            }
            else
            {
                return Json(new
                {
                    isRunning = false,
                    msg = "Выгрузка не зарегистрирована!"
                });
            }
        }

    }
}