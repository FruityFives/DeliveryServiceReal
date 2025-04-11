using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorkerService.Models;

public class ShippingrequestDTO
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string CustomerName { get; set; }
    public string PickupAddress { get; set; }
    public string PackageId { get; set; } = Guid.NewGuid().ToString();
    public string DeliveryAddress { get; set; }
    public string DeliveryDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
}

