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

    private void Write(string category, string level, string message, object? data)
    {
        var dir = Path.Combine(_basePath, category);
        Directory.CreateDirectory(dir);

        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var file = Path.Combine(dir, $"{date}.log");

        RotateIfNeeded(file);

        var json = JsonSerializer.Serialize(new
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Level = level,
            Message = message,
            Data = data
        });

        File.AppendAllText(file, json + Environment.NewLine);
    }

    private void RotateIfNeeded(string file)
    {
        if (!File.Exists(file)) return;

        var size = new FileInfo(file).Length;
        if (size < MaxSize) return;

        var newName = file.Replace(".log", $"_{DateTime.Now:HHmmss}.log");
        File.Move(file, newName);
    }
}
