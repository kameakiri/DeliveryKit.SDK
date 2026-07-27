namespace DeliveryKit.Core;

public class DeliveryInfo
{
    public string DeliveryId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
