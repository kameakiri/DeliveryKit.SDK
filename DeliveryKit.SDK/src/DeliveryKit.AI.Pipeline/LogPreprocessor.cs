namespace DeliveryKit.AI.Pipeline;

public class LogPreprocessor
{
    public IEnumerable<string> Normalize(IEnumerable<string> logs)
    {
        foreach (var line in logs)
        {
            yield return line.Trim();
        }
    }
}
