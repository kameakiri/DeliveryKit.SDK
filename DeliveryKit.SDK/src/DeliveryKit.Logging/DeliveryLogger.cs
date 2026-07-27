using DeliveryKit.Logging;
using System.Text.Json;

public class DeliveryLogger : IDeliveryLogger
{
    private readonly string _basePath = @"C:\DeliveryKit\Api\Logs";
    private readonly string _category;

    private const long MaxSize = 1_000_000; // 1MB

    // ★ 引数1つのコンストラクタを追加
    public DeliveryLogger(string category = "core")
    {
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
