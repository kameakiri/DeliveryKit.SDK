namespace DeliveryKit.Logging;

public interface IDeliveryLogger
{
    void Info(string message, object? data = null);
    void Warn(string message, object? data = null);
    void Error(string message, object? data = null);
}
