namespace DeliveryKit.Core;

public class DeliveryService : IDeliveryService
{
    private readonly Dictionary<string, DeliveryInfo> _store = new();

    public DeliveryResult CreateDelivery(CreateDeliveryRequest request)
    {
        var id = Guid.NewGuid().ToString();

        var info = new DeliveryInfo
        {
            DeliveryId = id,
            OrderId = request.OrderId,
            Address = request.Address,
            CreatedAt = DateTime.Now
        };

        _store[id] = info;

        return new DeliveryResult
        {
            Success = true,
            Message = "Delivery created",
            Delivery = info
        };
    }

    public DeliveryInfo? GetDelivery(string id)
    {
        return _store.TryGetValue(id, out var info) ? info : null;
    }
}
