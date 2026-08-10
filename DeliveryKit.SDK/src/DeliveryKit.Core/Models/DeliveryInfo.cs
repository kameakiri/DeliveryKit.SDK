namespace DeliveryKit.Core;

public class DeliveryInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Address { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DeliveryOrderRequest Order { get; set; } = new();
    public DeliveryPackageRequest Package { get; set; } = new();

    // サンプル実装では固定文字列のみ。本家のようなステータス遷移
    // （Pending→Preparing→InTransit→...）は含めていない。
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
