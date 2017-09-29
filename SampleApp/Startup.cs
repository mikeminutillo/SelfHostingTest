using System;
using System.Diagnostics;
using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using NServiceBus.Serilog.Tracing;
using Raven.Client.Document;
using Serilog;
using SampleApp.Messages.File;

namespace SampleApp
{
    class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IEndpointInstance EndpointInstance { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            Configuration = configBuilder.Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.Register(x => Configuration).As<IConfigurationRoot>();

            builder.Register(x => EndpointInstance).As<IEndpointInstance>().SingleInstance();

            builder.RegisterType<FileSaga>();
                
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            var config = new EndpointConfiguration("SampleEndpoint");

            config.EnableFeature<Sagas>();

            config.UseSerialization<XmlSerializer>();

            var transport = config.UseTransport<MsmqTransport>();

            // Routing

            // Load configuration

            var documentStore = new DocumentStore()
            {
                Url = "http://localhost:8081",
                DefaultDatabase = "Test"
            };

            var persistence = config.UsePersistence<RavenDBPersistence>();

            persistence.UseDocumentStoreForGatewayDeduplication(documentStore);
            persistence.UseDocumentStoreForSagas(documentStore);
            persistence.UseDocumentStoreForSubscriptions(documentStore);
            persistence.UseDocumentStoreForTimeouts(documentStore);

            config.SendFailedMessagesTo("error");

            config.UseContainer<AutofacBuilder>(
                customizations: customizations =>
                {
                    customizations.ExistingLifetimeScope(ApplicationContainer);
                });

            config.EnableFeature<TracingLog>();

            var conventions = config.Conventions();
            conventions.DefiningMessagesAs(
                type =>
                {
                    return type.Namespace == "SampleApp.Messages.File";
                });
            config.SerilogTracingTarget(Log.Logger);
            LogManager.Use<SerilogFactory>();

            EndpointInstance = await Endpoint.Start(config).ConfigureAwait(false);
            if(Debugger.IsAttached)
            {
                await EndpointInstance.SendLocal(new StartFileImportMessage { FileName = "SampleFile" }).ConfigureAwait(false);
            }
        }
    }
}
