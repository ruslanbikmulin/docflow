﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Docflow.Impl;
using Docflow.Interface;

namespace Docflow.Container
{
    public class AppCastleInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IUploadService>().ImplementedBy(typeof(UploadService)).LifestyleTransient());
        }
    }
}