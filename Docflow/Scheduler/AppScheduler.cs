using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Quartz;

namespace Docflow.Scheduler
{
    public static class AppScheduler
    {
        public static IScheduler LocalScheduler { get; set; }

        public static void InitScheduler()
        {
            var properties = new NameValueCollection { { "quartz.threadPool.threadCount", "1" } };

            ISchedulerFactory schedFact = new Quartz.Impl.StdSchedulerFactory(properties);
            LocalScheduler = schedFact.GetScheduler();
            LocalScheduler.Start();
        }
    }
}