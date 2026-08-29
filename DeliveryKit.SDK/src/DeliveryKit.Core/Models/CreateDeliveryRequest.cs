namespace DeliveryKit.Core;

// 本家 DeliveryKit.Api の /api/delivery/create と同じ形（Address/RecipientName/
// RecipientPhone + Order/Package のネストDTO）に揃えている。これは実装の中身
// （プラン管理・キャリア連携・DB永続化等）を見せるためではなく、「弊社のAPIは
// こういう形でリクエストを受け取る」という契約をサンプルとして正しく示すため。
// 内部主キー（Id等）をこのDTOに含めないのも本家と同じ意図（マスアサインメント対策）。
public class CreateDeliveryRequest
{
    public string Address { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DeliveryOrderRequest Order { get; set; } = new();
    public DeliveryPackageRequest Package { get; set; } = new();

    // 監査で発覚（High）：このテンプレートには冪等性キーが無く、二重クリックや
    // ネットワーク再送で同じ配送が複数作成され得た。本家DeliveryKit.Api.Controllers.
    // DeliveryController.CreateDelivery（DeliveryDbService.AddIfNotExistsAsync）と
    // 同じ方針で、クライアントが1操作ごとに生成するGuidを必須にする。
    public Guid? IdempotencyKey { get; set; }
}

public class DeliveryOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public DateTime RequestedDeliveryDate { get; set; }
}

public class DeliveryPackageRequest
{
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Depth { get; set; }
    public string Description { get; set; } = string.Empty;
}
