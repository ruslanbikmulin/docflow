﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Docflow.Container;
using Docflow.Interface;

namespace Docflow
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            var appChecker = AppContainer.Container.Resolve<IAppEnvironmentChecker>();

            if (appChecker.AppReadyToWork)
            {
                routes.MapRoute(
                    name: "Default",
                    url: "{controller}/{action}/{id}",
                    defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                );
            }
            else
            {
                routes.MapRoute(
                    name: "Default",
                    url: "{controller}/{action}/{id}",
                    defaults: new { controller = "Home", action = "Error", id = UrlParameter.Optional }
                );
            }

            
        }
    }
}
