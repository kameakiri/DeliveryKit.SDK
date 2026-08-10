namespace DeliveryKit.Core;

// これはSDKサンプル用の簡易実装（インメモリ、DB永続化なし）である。
// 弊社の実プロダクト（DeliveryKit.Core本体）にはプラン別機能・キャリア連携・
// EF Core永続化・IDOR対策等が別途あるが、それらはこのSDKには含めていない
// （Pro/Enterprise向けSDKは「開発の型」を示すサンプルであり、製品ロジックそのものの提供ではない）。
//
// ただし、入力検証のような「セキュリティ上の型」自体は製品固有のロジックではなく
// 一般的なベストプラクティスなので、サンプルであってもきちんと示している。
// 本番投入時は最低限、認証・永続化・監査ログの追加を必ず検討すること。
//
// DIへの登録は必ず Singleton にすること（Scopedにすると、リクエストのたびに
// このインスタンスごと作り直され、_store が毎回空になって「作成した配送が
// GETで見つからない」という不具合になる）。ここでは複数リクエストからの
// 同時アクセスに耐えるよう ConcurrentDictionary を使っている。
public class DeliveryService : IDeliveryService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DeliveryInfo> _store = new();

    public DeliveryResult CreateDelivery(CreateDeliveryRequest request)
    {
        try
        {
            Validate(request);
        }
        catch (DeliveryValidationException ex)
        {
            return new DeliveryResult
            {
                Success = false,
                Message = $"{ex.FieldName}: {ex.Message}"
            };
        }

        var info = new DeliveryInfo
        {
            Address = request.Address,
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            Notes = request.Notes,
            Order = request.Order,
            Package = request.Package,
        };

        _store[info.Id] = info;

        return new DeliveryResult
        {
            Success = true,
            Message = "Delivery created",
            Delivery = info
        };
    }

    public DeliveryInfo? GetDelivery(string id)
    {
        return Guid.TryParse(id, out var guid) && _store.TryGetValue(guid, out var info)
            ? info
            : null;
    }

    private static void Validate(CreateDeliveryRequest request)
    {
        RequireNonEmpty(request.Address, nameof(request.Address));
        RequireNonEmpty(request.RecipientName, nameof(request.RecipientName));
        RequireNonEmpty(request.RecipientPhone, nameof(request.RecipientPhone));

        RejectControlCharacters(request.Address, nameof(request.Address));
        RejectControlCharacters(request.RecipientName, nameof(request.RecipientName));
        RejectControlCharacters(request.Notes, nameof(request.Notes));
    }

    private static void RequireNonEmpty(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DeliveryValidationException(fieldName, "must not be empty.");
    }

    // ログやラベルにそのまま出力される値に改行等の制御文字が混ざると、
    // ログ改ざん・インジェクションの起点になり得るため拒否する
    // （タブのみ許容。本家 DeliveryKit.Core と同じ考え方）。
    private static void RejectControlCharacters(string? value, string fieldName)
    {
        if (value == null) return;

        foreach (var c in value)
        {
            if (char.IsControl(c) && c != '\t')
                throw new DeliveryValidationException(fieldName, "must not contain control characters.");
        }
    }
}
