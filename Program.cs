using NLog;
using NLog.Web;
using ServiceWorker;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

logger.Debug("Start min service");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>(); // Din BackgroundService
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders(); // Fjerner standard loggere (f.eks. ConsoleLogger)
    })
    .UseNLog() // Tilføjer NLog som logger
    .Build();

host.Run();
