using DeliveryKit.Core;
using DeliveryKit.Logging;

namespace BasicDeliveryApp;

public class Program
{
    public static void Main(string[] args)
    {
        // basePath省略時は実行ディレクトリ配下の "Logs"。
        var logger = new DeliveryLogger(category: "sample");
        var service = new DeliveryService();

        var request = new CreateDeliveryRequest
        {
            Address = "東京都千代田区1-1-1",
            RecipientName = "山田太郎",
            RecipientPhone = "090-0000-0000",
            Order = new DeliveryOrderRequest
            {
                OrderId = "SAMPLE-001",
                SenderName = "サンプル株式会社",
                SenderAddress = "大阪府大阪市1-1-1",
                RecipientName = "山田太郎",
                RecipientAddress = "東京都千代田区1-1-1",
                RequestedDeliveryDate = DateTime.UtcNow
            },
            Package = new DeliveryPackageRequest
            {
                Weight = 1.5m,
                Height = 20,
                Width = 15,
                Depth = 10,
                Description = "サンプル荷物"
            }
        };

        // 監査で発覚：request全体（住所・受取人氏名・電話番号等）をそのままログへ書き込むと、
        // このサンプル自体は固定のダミーデータだが、複製先で実データに差し替えられた際に
        // PIIをログへ平文で残す実装がそのままコピーされてしまう（DeliveryKit.ApiTemplate.
        // Controllers.DeliveryControllerと同じ理由・同じ修正）。
        logger.Info("Sample app: CreateDelivery called", new { orderId = request.Order.OrderId });

        var result = service.CreateDelivery(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Message: {result.Message}");
        Console.WriteLine($"DeliveryId: {result.Delivery?.Id}");
    }
}
