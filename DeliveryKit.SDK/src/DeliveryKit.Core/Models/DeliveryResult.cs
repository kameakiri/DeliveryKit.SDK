namespace DeliveryKit.Core;

public class DeliveryResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DeliveryInfo? Delivery { get; set; }

    /// <summary>
    /// 検証に失敗した項目の名前。成功時は null。
    ///
    /// 監査で発覚：**これが無かったせいで、API側が項目名を取り出せなかった。**
    /// DeliveryValidationException は FieldName を持っているのに、
    /// CreateDelivery がそれを Message の先頭へ文字列として畳み込んでしまい
    /// （"Address: must not be empty."）、呼び出し側は文字列を切り出すしかなかった。
    ///
    /// その結果 DeliveryKit.ApiTemplate は、**同じエンドポイントから2つの形の
    /// エラーを返していた。**
    ///   - モデルのバインド失敗（Program.cs の InvalidModelStateResponseFactory）
    ///     → { error, field }。本家DeliveryKitの全APIと同じ形
    ///   - この検証の失敗 → { success, message, delivery }
    /// 呼び出し側は1つのエンドポイントに2通りの分岐を書くことになる。
    /// テンプレートは「開発の型」を配るものなので、その形が2つあってはいけない。
    ///
    /// Message はこれまでどおり "項目名: 内容" のまま残してある
    /// （サンプルアプリや docs/core.md が参照しているため）。
    /// </summary>
    public string? Field { get; set; }
}
