namespace Docflow
{
    using Castle.Windsor;
    using Docflow.Container;
    using Docflow.Interface;
    using Docflow.Logging;
    using Docflow.Scheduler;
    using NLog;
    using System;
    using System.Web;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Routing;

    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            AppLogging.Logger = LogManager.GetCurrentClassLogger();
            AppContainer.Container = new WindsorContainer();
            AppContainer.Container.Install(new AppCastleInstaller());
            
            AreaRegistration.RegisterAllAreas();

            var appChecker = AppContainer.Container.Resolve<IAppEnvironmentChecker>();
            appChecker.CheckSettings();

            GlobalConfiguration.Configure(WebApiConfig.Register);

            RouteConfig.RegisterRoutes(RouteTable.Routes);      
            AppScheduler.InitScheduler();

            AppContainer.Container.Resolve<ITaskResumer>().Resume();
        }
    }
}