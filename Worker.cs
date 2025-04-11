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
        var mongoClientHost = _configuration["MongoDB_HOST"] ?? "mongo not set";
        _logger.LogInformation($"RabbitMQ host: {rabbitMQHost}");
        _logger.LogInformation($"MongoDB url: {mongoClientHost}");

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
                _logger.LogInformation($" [x] Received: {message}");

                try
                {
                    var shippingRequest = JsonSerializer.Deserialize<ShippingrequestDTO>(message);

                    if (shippingRequest == null)
                    {
                        _logger.LogError("Deserialization returned null.");
                        return;
                    }

                    _logger.LogInformation($"Deserialized object: {JsonSerializer.Serialize(shippingRequest)}");

                    _logger.LogInformation("Attempting MongoDB connection...");
                    var mongoDbClient = new MongoClient(mongoClientHost);
                    var database = mongoDbClient.GetDatabase("ShippingDb");
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
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization failed.");
                }
                catch (MongoException mongoEx)
                {
                    _logger.LogError(mongoEx, "MongoDB insert failed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while processing message.");
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
            _logger.LogError(ex, "An error occurred while setting up RabbitMQ connection.");
        }

        _logger.LogInformation("Worker stopped");
    }
}
