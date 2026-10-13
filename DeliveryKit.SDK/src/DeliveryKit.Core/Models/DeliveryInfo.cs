namespace DeliveryKit.Core;

public class DeliveryInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>外部システムの注文番号。便に1つ。</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>送り主名。便に1つ。</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>送り主住所。便に1つ。</summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>希望配送日。便に1つ。</summary>
    public DateTime RequestedDeliveryDate { get; set; }

    /// <summary>便全体にかかる備考。軒ごとの事情は届け先側の Notes にある。</summary>
    public string? Notes { get; set; }

    /// <summary>配送業者の識別子（"yamato" / "sagawa" / "japanpost"）。</summary>
    public string CarrierId { get; set; } = string.Empty;

    /// <summary>
    /// 届け先（1つ以上）。**受け取った順に回る番号が振られる。**
    ///
    /// 以前このクラスは受取人・住所を直接持っていた（配送1件＝届け先1つ）。
    /// 本家が複数の届け先を持つ形へ変わったので、ここも合わせてある
    /// （CreateDeliveryRequest のコメント参照）。
    /// </summary>
    public List<DeliveryDestinationInfo> Destinations { get; set; } = new();

    // サンプル実装では固定文字列のみ。本家のようなステータス遷移
    // （Pending→Preparing→InTransit→...）や、**届け先ごとの状態から便全体の
    // 状態を導く仕組み**（本家 DeliveryStatusRollup。3軒のうち1軒だけ渡せなければ
    // 「一部完了」）は含めていない。
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>届け先1軒ぶん（サンプルの保持用）。</summary>
public class DeliveryDestinationInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>回る順番。**サーバー側で配列の順に振る**（本家と同じ）。</summary>
    public int SequenceNumber { get; set; }

    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<DeliveryPackageRequest> Packages { get; set; } = new();
}
