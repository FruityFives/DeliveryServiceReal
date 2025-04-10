using NLog;
using NLog.Web;
using ServiceWorker;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    logger.Debug("Start min service");

    IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddHostedService<Worker>(); // Din BackgroundService
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders(); // Fjerner default loggere
        })
        .UseNLog() // Aktiverer NLog som logger
        .Build();

    logger.Info("🔥 TEST log from shipping-service"); // ✅ Now this runs immediately

    host.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw; // Sørg for at fejlen ikke bliver slugt
}
finally
{
    LogManager.Shutdown(); // Lukker logging-systemet ned korrekt
}
