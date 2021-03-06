﻿using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Responses;
using Nancy.Session;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CWSWeb
{
    public class ApplicationBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(Nancy.Embedded.Conventions.EmbeddedStaticContentConventionBuilder.AddDirectory("Static", GetType().Assembly));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Files", "Files", ".png"));

            base.ConfigureConventions(nancyConventions);
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            Assembly assembly = GetType().Assembly;
            ResourceViewLocationProvider.RootNamespaces.Add(assembly, "CWSWeb.Views");
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
            }
        }

        void OnConfigurationBuilder(NancyInternalConfiguration x)
        {
            x.ViewLocationProvider = typeof (ResourceViewLocationProvider);
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            var formsAuthConfiguration =
                new FormsAuthenticationConfiguration()
                {
                    RedirectUrl = "~/login",
                    UserMapper = container.Resolve<IUserMapper>(),
                };


            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
            CookieBasedSessions.Enable(pipelines);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx =>
                {
                    if (Helper.Settings.Instance.Client.CanConnect)
                        return null;
                    else
                    {
                        if (String.Compare(ctx.Request.Path, "/notavailable", true) == 0)
                        {
                            return null;
                        }
                        return new RedirectResponse("/notavailable");
                    }
                });

            base.RequestStartup(container, pipelines, context);
        }
    }
}
