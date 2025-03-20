using WorkerService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ServiceWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started");
        var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "hostname = not set";
        _logger.LogInformation($"RabbitMQ host: {rabbitMQHost}");
        try
        {
            // Opret forbindelse og kanal med korrekt konfiguration
            var factory = new ConnectionFactory
            {
                HostName = rabbitMQHost,  // miljøvariable
                Port = 5672,  // Default port for RabbitMQ
                UserName = "guest",  // Default RabbitMQ brugernavn
                Password = "guest"   // Default RabbitMQ password
            };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "shippingQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            _logger.LogInformation(" [*] Waiting for messages.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation($" [x] Received {message}");

                var shippingRequest = JsonSerializer.Deserialize<ShippingrequestDTO>(message);
                ProcessShippingRequest(shippingRequest);

                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync("shippingQueue", autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while connecting to RabbitMQ");
        }

        _logger.LogInformation("Worker stopped");
    }

    private void ProcessShippingRequest(ShippingrequestDTO request)
    {
        string csvFilePath = "shippingRequests.csv";
        using (var writer = new StreamWriter(csvFilePath, append: true))
        {
            string line = $"{request.CustomerName},{request.PickupAddress},{request.PackageId},{request.DeliveryAddress},{request.Date}";
            writer.WriteLine(line);
        }
    }
}