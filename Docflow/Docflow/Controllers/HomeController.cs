﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Docflow.Container;
using Docflow.DAL.EntityContext;
using Docflow.DAL.Models;
using Docflow.DtoEntities;
using Docflow.Enums;
using Docflow.Extensions;
using Docflow.Interface;
using Docflow.Logging;

namespace Docflow.Controllers
{
    public class HomeController : Controller
    {
        private Dictionary<UploadStatus, string> UploadStatusNames = new Dictionary<UploadStatus, string>()
        {
            {UploadStatus.Starting, "В процессе подготовки к выгрузке" },
            {UploadStatus.Uploading, "В процессе выгрузки сканов" },
            {UploadStatus.FileInfoFilling, "В процессе сбора информации о путях до сканов" },
            {UploadStatus.Zipping, "В процессе архивирования договоров" },
            {UploadStatus.Paused, "В процессе ожидания разрешенного времени для работы" },
            {UploadStatus.Canceling, "В процессе прерывания" },
            {UploadStatus.Finishing, "В процессе завершения" }
        };
        
        public ActionResult Index()
        {
            var uploadService = AppContainer.Container.Resolve<IUploadService>();
            var upload = uploadService.GetRunningUpload();

            ViewData["uploadIsRunning"] = upload != null;
            ViewData["uploadStatus"] = upload == null ? "Нет загрузок".AddQuotes() : UploadStatusNames[upload.UploadStatus].AddQuotes();
            ViewData["uploadStart"] = upload == null ? " ".AddQuotes() : upload.UploadStartDate.ToString().AddQuotes();
            ViewData["userNameStart"] = upload == null ? " ".AddQuotes() : upload.UserNameStart.AddQuotes();
            ViewData["contractCount"] = upload == null ? 0 : upload.ContractCount;
            ViewData["contractUploadedCount"] = upload == null ? 0 : upload.UploadProgressRows.Count(u => u.ProgressStatus == ProgressStatus.Uploaded);
            ViewData["contractZippedCount"] = upload == null ? 0 : upload.ZippedCount;
            ViewData["contractErrorCount"] = upload == null ? 0 : upload.ErrorCount;

            return View();
        }

        public ActionResult Error()
        {
            // возможно стоит прокинуть подробности ошибок.
            return View();
        }
    }
}