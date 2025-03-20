using WorkerService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ServiceWorker;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started");

        // 1. <indsæt noget RabbitMQ Query + serialiserings kode her!>
        // 2. <Tilføj BookingDTO fra køen til lokal Repository-klasse!>

        List<ShippingrequestDTO> shippingRequests = new List<ShippingrequestDTO>
        {
            new ShippingrequestDTO 
            { 
                CustomerName = "John Doe", 
                PickupAddress = "123 Main St", 
                DeliveryAddress = "456 Elm St" 
            },
            new ShippingrequestDTO 
            { 
                CustomerName = "Jane Smith", 
                PickupAddress = "789 Oak St", 
                DeliveryAddress = "101 Pine St" 
            },
            new ShippingrequestDTO 
            { 
                CustomerName = "Bob Johnson", 
                PickupAddress = "234 Maple St", 
                DeliveryAddress = "567 Birch St" 
            }
        };
        
        string csvFilePath = "shippingRequests.csv";
        using (var writer = new StreamWriter(csvFilePath))
        {
            writer.WriteLine("CustomerName,PickupAddress,PackageId,DeliveryAddress,Date");
            foreach (var request in shippingRequests)
            {
                string line = $"{request.CustomerName},{request.PickupAddress},{request.PackageId},{request.DeliveryAddress},{request.Date}";
                writer.WriteLine(line);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.LogInformation("Worker stopped working");
    }
}