using DeliveryKit.Logging;
using System.Text.Json;

public class DeliveryLogger : IDeliveryLogger
{
    private readonly string _basePath;
    private readonly string _category;

    private const long MaxSize = 1_000_000; // 1MB

    // 以前は basePath が C:\DeliveryKit\Api\Logs にハードコードされており、
    // かつ唯一のコンストラクタ引数が実際には category にバインドされていたため
    // （呼び出し側は basePath のつもりで渡していた＝サンプル内で既に矛盾していた）、
    // basePath を渡しても無視される状態だった。
    // 配布可能なSDKが特定ドライブ・特定ユーザー構成の絶対パスを前提にするのは
    // そもそも不適切なため、既定値は実行ディレクトリ配下の相対パスにした。
    public DeliveryLogger(string? basePath = null, string category = "core")
    {
        _basePath = basePath ?? Path.Combine(AppContext.BaseDirectory, "Logs");
        _category = category;
    }

    public void Info(string message, object? data = null)
        => Write(_category, "INFO", message, data);

    public void Warn(string message, object? data = null)
        => Write(_category, "WARN", message, data);

    public void Error(string message, object? data = null)
        => Write("error", "ERROR", message, data);

    // 監査で発覚：docs/logging.mdは「日本時間（JST）でのタイムスタンプ」と謳っているが、
    // 従来はDateTime.Now（実行環境のOSローカルタイムゾーン依存）をそのまま使っており、
    // タイムゾーンをJSTに固定するロジックが無かった。Azure App Service等UTC既定の
    // クラウド環境にデプロイすると、ドキュメント通りにはならずUTC（またはホストの設定
    // 次第の別のタイムゾーン）でタイムスタンプが記録されてしまう。日本には夏時間が無く
    // JSTは常にUTC+9で固定のため、TimeZoneInfoのID（Windowsは"Tokyo Standard Time"、
    // Linuxは"Asia/Tokyo"でOS間で異なり、かつ最小構成コンテナではtzdata自体が無く
    // TimeZoneNotFoundExceptionになり得る）に依存せず、UtcNowへの単純な+9時間で
    // 求める（配布先の実行環境を問わず確実に動く）。
    private static DateTime JstNow() => DateTime.UtcNow.AddHours(9);

    // 内部基盤(DeliveryKit.Log.LogWriter)で実際に発生・修正済みのレースと同種の不備が
    // このサンプルSDKには残っていた（監査で発覚）: docs/getting-started.mdはIDeliveryLogger
    // をAddSingletonで登録するよう案内しており、1プロセス内の全リクエストが同一インスタンス・
    // 同一ログファイルを共有する構成を推奨している。File.AppendAllTextは排他制御を一切
    // 行わないため、複数リクエストが同時に同じカテゴリ・同日のログファイルへ書き込もうと
    // するとIOException（ファイルロック競合）が発生し得る。ローテーション判定（サイズ確認→
    // リネーム）だけロックしても、判定後にもう一方のスレッドが書き込んでからロックしては
    // 意味が無いため、ディレクトリ作成からローテーション・書き込みまでを一連の排他区間にする
    // （LogWriter.Writeと同じ方針）。
    //
    // 既知の制約: このロックは.NETプロセス内でのみ有効。複数プロセスが同じログファイル
    // パスへ同時に書き込む構成には効かない（LogWriter.csの同種コメント、docs/logging.mdの
    // 追記参照）。
    private static readonly object WriteLock = new();

    private void Write(string category, string level, string message, object? data)
    {
        var dir = Path.Combine(_basePath, category);

        var json = JsonSerializer.Serialize(new
        {
            Timestamp = JstNow().ToString("yyyy-MM-dd HH:mm:ss"),
            Level = level,
            Message = message,
            Data = data
        });

        // 監査で発覚：内部基盤(DeliveryKit.Log.LogWriter)と同じ理由（ディスク容量枯渇・
        // 一時的なファイルロック等）でIOExceptionが起きると、呼び出し元のコントローラーの
        // 例外処理の最終段でこのメソッドを呼んでいる場合、本来返すはずだった応答すら
        // 返せない未処理例外に化けてしまう。ログ書き込み自体の失敗は、呼び出し元の実際の
        // 応答を壊してまで伝播させる価値が無いため、握りつぶしベストエフォートで
        // Console.Errorへ回す（LogWriter.Writeと同じ方針）。
        try
        {
            lock (WriteLock)
            {
                Directory.CreateDirectory(dir);

                var date = JstNow().ToString("yyyy-MM-dd");
                var file = Path.Combine(dir, $"{date}.log");

                RotateIfNeeded(file);

                File.AppendAllText(file, json + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            Console.Error.WriteLine($"[DeliveryKit.Logging] ログの書き込みに失敗しました（category={category}）。");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"[DeliveryKit.Logging] ログの書き込みに失敗しました（category={category}）。");
        }
    }

    // 監査で発覚：内部基盤(DeliveryKit.Log.LogWriter)は"_1.log, _2.log..."のように
    // 空き名をインクリメントしながら探すが、このサンプルSDKは秒単位の時刻
    // （_HHmmss.log）をそのまま使っており空き名チェックが無かった。同一カテゴリ・
    // 同一秒内に1MB超のログが2回以上ローテーションされる状況（高頻度ログ出力の
    // バースト等）では、2回目のFile.Moveが既存の同名ファイルに対して例外を投げうる
    // （WriteLock保持中のため例外は呼び出し元へそのまま伝播する）。LogWriter.Writeと
    // 同じ「空き名が見つかるまで探す」方式に揃える。
    private void RotateIfNeeded(string file)
    {
        if (!File.Exists(file)) return;

        var size = new FileInfo(file).Length;
        if (size < MaxSize) return;

        var index = 1;
        string newName;
        do
        {
            newName = file.Replace(".log", $"_{JstNow():HHmmss}_{index}.log");
            index++;
        }
        while (File.Exists(newName));

        File.Move(file, newName);
    }
}
