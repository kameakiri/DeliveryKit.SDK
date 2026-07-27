namespace DeliveryKit.Core;

public class DeliveryResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DeliveryInfo? Delivery { get; set; }
}
