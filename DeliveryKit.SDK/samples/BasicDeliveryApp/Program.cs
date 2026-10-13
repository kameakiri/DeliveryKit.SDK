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

        // **1便で複数の届け先を回る形**（本家 /api/delivery/create と同じ）。
        // 受取人・住所・荷物は**届け先ごと**に持ち、送り主と注文番号は便に1つ。
        // 以前このサンプルは届け先1つぶんを配送本体に書いていたが、それでは
        // 本家のAPIに通らない（CreateDeliveryRequest のコメント参照）。
        var request = new CreateDeliveryRequest
        {
            OrderId = "SAMPLE-001",
            SenderName = "サンプル株式会社",
            SenderAddress = "大阪府大阪市1-1-1",
            RequestedDeliveryDate = DateTime.UtcNow,
            Notes = "午前中の配達希望",
            Destinations = new List<CreateDeliveryDestinationRequest>
            {
                new()
                {
                    RecipientName = "山田太郎",
                    RecipientPhone = "090-0000-0000",
                    Address = "東京都千代田区1-1-1",
                    Packages = new List<DeliveryPackageRequest>
                    {
                        new() { Weight = 1.5m, Height = 20, Width = 15, Depth = 10, Description = "サンプル荷物" }
                    }
                },
                // **2軒目を書いてあるのは、1軒だけの形と見分けがつくようにするため。**
                // 1軒しか書いていないサンプルは、配列であることが読み飛ばされる。
                new()
                {
                    RecipientName = "鈴木花子",
                    RecipientPhone = "090-1111-1111",
                    Address = "東京都千代田区2-2-2",
                    Notes = "不在時は宅配ボックス",
                    Packages = new List<DeliveryPackageRequest>
                    {
                        new() { Weight = 0.8m, Height = 10, Width = 10, Depth = 5, Description = "サンプル荷物2" }
                    }
                }
            },
            // 監査で発覚：CreateDeliveryRequest.IdempotencyKeyが必須化されたため、
            // このサンプルも実際の呼び出し元（HTTPクライアント）と同じく1操作につき
            // 1回生成したGuidを渡す（DeliveryController.CreateDelivery参照）。
            IdempotencyKey = Guid.NewGuid()
        };

        // 監査で発覚：request全体（住所・受取人氏名・電話番号等）をそのままログへ書くと、
        // このサンプル自体は固定のダミーデータだが、複製先で実データに差し替えられた際に
        // PIIをログへ平文で残す実装がそのままコピーされてしまう（DeliveryKit.ApiTemplate.
        // Controllers.DeliveryControllerと同じ理由・同じ修正）。
        // **軒数は出す。** 件数は個人情報ではなく、取り違えにいちばん早く気づける値。
        logger.Info("Sample app: CreateDelivery called",
            new { orderId = request.OrderId, destinations = request.Destinations.Count });

        var result = service.CreateDelivery(request);

        Console.WriteLine($"Success: {result.Success}");
        Console.WriteLine($"Message: {result.Message}");
        Console.WriteLine($"DeliveryId: {result.Delivery?.Id}");
        foreach (var destination in result.Delivery?.Destinations ?? new List<DeliveryDestinationInfo>())
            Console.WriteLine($"  {destination.SequenceNumber}. {destination.RecipientName}（{destination.Packages.Count}口）");
    }
}
