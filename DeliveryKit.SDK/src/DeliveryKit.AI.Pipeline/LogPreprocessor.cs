namespace DeliveryKit.AI.Pipeline;

/// <summary>
/// AIモデルへの学習データ投入前に、ログ行を最低限クリーンアップするサンプル実装。
/// 実際の前処理（トークナイズ、機密情報のマスキング等）はモデル・用途に応じて
/// 利用側で拡張することを想定している。
/// </summary>
public class LogPreprocessor
{
    /// <summary>
    /// 各行の前後の空白をTrimし、空行（空白のみの行を含む）を除去する。
    /// </summary>
    public IEnumerable<string> Normalize(IEnumerable<string> logs)
    {
        foreach (var line in logs)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            yield return line.Trim();
        }
    }
}
