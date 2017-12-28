using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Docflow.Container;
using Docflow.Interface;
using Docflow.Jobs;
using Docflow.Logging;
using Docflow.Scheduler;
using Quartz;

namespace Docflow.Impl
{
    public class TaskResumer : ITaskResumer
    {
        public void Resume()
        {
            AppLogging.Logger.Debug("Попытка возобновить задачу выгрузки.");

            var uploadService = AppContainer.Container.Resolve<IUploadService>();

            var runningUpload = uploadService.GetRunningUpload();
            if (runningUpload != null)
            {
                AppLogging.Logger.Debug("Нашли зарегистрированную незавершенную выгрузку.");
                // надо возобновить задачу в планировщике
                var job = JobBuilder.Create<UnloadDocumentJob>().WithIdentity("worker").Build();

                AppLogging.Logger.Debug("Создаем возобновленную задачу в планировщике. Параметры: " + ConfigurationManager.AppSettings["SheduleSettings"]);
                // создаем крон триггер
                var trigger = TriggerBuilder.Create()
                    .WithIdentity("UploadTriggerIdentity")
                    // Задача будет запускаться каждый день
                    // Задача будет исполняться с 01 часа ночи до 03 часов ночи
                    // это означает что будет выделять процессорное время на выполнение кода в используемом jobe
                    .WithCronSchedule(ConfigurationManager.AppSettings["SheduleSettings"])
                    //.StartNow()
                    .Build();
                AppLogging.Logger.Debug("Создали возобновленную задачу в планировщике.");

                AppScheduler.LocalScheduler.ScheduleJob(job, trigger);
                AppLogging.Logger.Debug("Зарегистрировали задачу.");
            }
            else
            {
                AppLogging.Logger.Debug("Нет задач для возобновления");
            }
        }
    }
}