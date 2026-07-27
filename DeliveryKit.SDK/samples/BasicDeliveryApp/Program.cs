using DeliveryKit.Core;
using DeliveryKit.Logging;

namespace BasicDeliveryApp;

public class Program
{
    public static void Main(string[] args)
    {
        var logger = new DeliveryLogger(@"C:\DeliveryKit\Api\Logs");
        var service = new DeliveryService();

        var request = new CreateDeliveryRequest
        {
            OrderId = "SAMPLE-001",
            Address = "東京都千代田区",
            RequestedDate = DateTime.Now
        };

        logger.Info("Sample app: CreateDelivery called", new { request });

        var result = service.CreateDelivery(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"DeliveryId: {result.Delivery?.DeliveryId}");
    }
}
