namespace DeliveryKit.Core;

// 本家 DeliveryKit.Api の /api/delivery/create と同じ形に揃えている。これは実装の中身
// （プラン管理・キャリア連携・DB永続化等）を見せるためではなく、「弊社のAPIは
// こういう形でリクエストを受け取る」という契約をサンプルとして正しく示すため。
// 内部主キー（Id等）をこのDTOに含めないのも本家と同じ意図（マスアサインメント対策）。
//
// **監査で発覚：ここが古い形のまま残っていた。**
// 以前の本家は「配送1件＝届け先1つ」で、受取人・住所を配送本体に持ち、さらに
// `Order` の中にも受取人・住所を持っていた（ラベルPDFとキャリアAPIが別々の列を
// 見ている状態で、本家側で一本化された）。本家は**配送1件が届け先を複数持つ形**へ
// 変わり、受取人・住所・荷物は届け先ごとになった（送り主と注文番号は便に1つ）。
//
// このSDKはその変更に追従しておらず、**「本家と同じ形」と書いたまま違う形**を
// 顧客へ配っていた。古い形で本家の /api/delivery/create を呼ぶと
// 「届け先を1つ以上指定してください。」（field: destinations）で弾かれる。
// 契約を示すことがこのクラスの存在理由なので、形そのものを合わせる
// （中の実装はメモリ保持の簡易なままでよい）。
public class CreateDeliveryRequest
{
    /// <summary>外部システムの注文番号。便に1つ。</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>送り主名。便に1つ。</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>送り主住所。便に1つ。本家ではルートの出発地にもなる。</summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>希望配送日。便に1つ。</summary>
    public DateTime RequestedDeliveryDate { get; set; }

    /// <summary>便全体にかかる備考。軒ごとの事情は届け先側の Notes に書く。</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 配送を委託する配送業者の識別子（"yamato" / "sagawa" / "japanpost"）。
    ///
    /// **本家APIでは省略できない。** 空文字や未対応の値を送ると
    /// 「対応していないキャリアです。」で弾かれる（CarrierClientFactory.GetClient）。
    /// このSDKは配送業者への登録そのものを行わない簡易実装だが、
    /// **契約として必要な項目なので持たせてある**——ここに無いと、
    /// サンプルどおりに書いた顧客が本家で弾かれる。
    /// </summary>
    public string CarrierId { get; set; } = string.Empty;

    /// <summary>
    /// **届け先（1つ以上）。** 本家は受け取った配列の順に回る番号を振る。
    /// 1台の車が1日に何十軒も回る実際の形に合わせてある。
    /// </summary>
    public List<CreateDeliveryDestinationRequest> Destinations { get; set; } = new();

    // 監査で発覚（High）：このテンプレートには冪等性キーが無く、二重クリックや
    // ネットワーク再送で同じ配送が複数作成され得た。本家DeliveryKit.Api.Controllers.
    // DeliveryController.CreateDelivery（DeliveryDbService.AddIfNotExistsAsync）と
    // 同じ方針で、クライアントが1操作ごとに生成するGuidを必須にする。
    public Guid? IdempotencyKey { get; set; }
}

/// <summary>
/// 届け先1軒ぶん。クライアントが指定してよい項目だけを持つ
/// （内部主キーと回る順番は、本家と同じくサーバーが決める）。
/// </summary>
public class CreateDeliveryDestinationRequest
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>この届け先についての備考（置き配の指定など）。</summary>
    public string? Notes { get; set; }

    /// <summary>この届け先へ渡す荷物（1つ以上）。1軒に複数口あってよい。</summary>
    public List<DeliveryPackageRequest> Packages { get; set; } = new();
}

public class DeliveryPackageRequest
{
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Depth { get; set; }
    public string Description { get; set; } = string.Empty;
}
