namespace WorkerService.Models;

public class ShippingrequestDTO
{
    public string CustomerName { get; set; }
    public string PickupAddress { get; set; }
    public string PackageId { get; set; } = Guid.NewGuid().ToString();
    public string DeliveryAddress { get; set; }

    public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
}