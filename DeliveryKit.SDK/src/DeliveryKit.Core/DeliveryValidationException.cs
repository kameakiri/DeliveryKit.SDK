namespace DeliveryKit.Core;

/// <summary>
/// 入力値検証エラーを表す例外。<see cref="FieldName"/> にどの項目が
/// 不正だったかを保持する（APIコントローラー側で400応答に使うことを想定）。
/// </summary>
public class DeliveryValidationException : Exception
{
    public string FieldName { get; }

    public DeliveryValidationException(string fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }
}
