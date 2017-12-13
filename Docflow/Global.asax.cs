using Docflow.Logging;
using Docflow.Scheduler;
using NLog;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;
using Docflow.Container;

namespace Docflow
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            AppContainer.Container = new WindsorContainer();
            AppContainer.Container.Install(new AppCastleInstaller());

            AppLogging.Logger = LogManager.GetCurrentClassLogger();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);      
            AppScheduler.InitScheduler();



        }
    }
}