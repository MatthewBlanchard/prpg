using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Crpg.Application.System.Commands;
using Crpg.Logging;
using Crpg.Persistence;
using Crpg.Sdk.Abstractions.Events;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LoggerFactory = Crpg.Logging.LoggerFactory;

namespace Crpg.WebApi
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            LoggerFactory.Initialize(configuration);

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .UseLogging()
                .Build();

            ILogger logger = LoggerFactory.CreateLogger<Program>();
            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                var mediator = services.GetRequiredService<IMediator>();
                var eventRaiser = services.GetRequiredService<IEventService>();
                var db = services.GetRequiredService<CrpgDbContext>();

                if (env == Environments.Production)
                {
                    try
                    {
                        await db.Database.MigrateAsync();
                        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                        conn.Open();
                        conn.ReloadTypes();
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "An error occurred while migrating the database.");
                        LoggerFactory.Close();
                        return 1;
                    }
                }

                var res = await mediator.Send(new SeedDataCommand(), CancellationToken.None);
                if (res.Errors != null)
                {
                    LoggerFactory.Close();
                    return 1;
                }

                eventRaiser.Raise(EventLevel.Info, "cRPG Web API has started", string.Empty);
            }

            try
            {
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                LoggerFactory.Close();
            }
        }
    }
}
