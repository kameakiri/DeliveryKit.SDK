namespace DeliveryKit.Core;

public class CreateDeliveryRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
}
