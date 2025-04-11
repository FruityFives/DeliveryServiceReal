using WorkerService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using MongoDB.Driver;

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
        var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "host not set";
        _logger.LogInformation($"RabbitMQ host: {rabbitMQHost}");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = rabbitMQHost,
                Port = 5672,
                UserName = "guest",
                Password = "guest"
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
                if (shippingRequest != null)
                {
                    var mongoClient = new MongoClient("mongodb://mongodb:27018");
                    var database = mongoClient.GetDatabase("ShippingDb");
                    var collection = database.GetCollection<ShippingrequestDTO>("ShippingRequests");

                    var shippingRecord = new ShippingrequestDTO
                    {
                        CustomerName = string.IsNullOrEmpty(shippingRequest.CustomerName) ? "N/A" : shippingRequest.CustomerName,
                        PickupAddress = string.IsNullOrEmpty(shippingRequest.PickupAddress) ? "N/A" : shippingRequest.PickupAddress,
                        PackageId = shippingRequest.PackageId,
                        DeliveryAddress = string.IsNullOrEmpty(shippingRequest.DeliveryAddress) ? "N/A" : shippingRequest.DeliveryAddress,
                        DeliveryDate = string.IsNullOrEmpty(shippingRequest.DeliveryDate) ? DateTime.Now.ToString("yyyy-MM-dd") : shippingRequest.DeliveryDate
                    };

                    await collection.InsertOneAsync(shippingRecord);
                    _logger.LogInformation("Shipping request saved to MongoDB.");
                }
                else
                {
                    _logger.LogError("Failed to deserialize the message into ShippingrequestDTO");
                }

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
}
