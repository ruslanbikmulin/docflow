using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.Windsor;

namespace Docflow.Container
{
    public static class AppContainer
    {
        public static  IWindsorContainer Container { get; set; }
    }
}