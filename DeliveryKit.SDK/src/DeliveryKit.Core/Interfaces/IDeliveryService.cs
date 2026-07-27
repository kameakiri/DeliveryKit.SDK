namespace DeliveryKit.Core;

public interface IDeliveryService
{
    DeliveryResult CreateDelivery(CreateDeliveryRequest request);
    DeliveryInfo? GetDelivery(string id);
}
