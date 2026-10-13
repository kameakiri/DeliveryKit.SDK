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

    // idempotencyKey -> 作成済みDeliveryInfo.Id。二重クリック・ネットワーク再送に
    // よる重複作成を防ぐ（監査で発覚。本家DeliveryKit.Api.Services.DeliveryDbService.
    // AddIfNotExistsAsyncと同じ考え方）。DB永続化が無いサンプルのため、SERIALIZABLE
    // トランザクションの代わりに単純なlockでcheck-then-actを保護する。
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Guid> _idempotencyIndex = new();
    private readonly object _createLock = new();

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
                // Field も入れる。API側が項目名を文字列から切り出さずに済む
                // （DeliveryResult.Field のコメント参照）。
                Field = ex.FieldName,
                Message = $"{ex.FieldName}: {ex.Message}"
            };
        }

        var idempotencyKey = request.IdempotencyKey!.Value;

        lock (_createLock)
        {
            if (_idempotencyIndex.TryGetValue(idempotencyKey, out var existingId) &&
                _store.TryGetValue(existingId, out var existingInfo))
            {
                // 同じidempotencyKeyの配送が既に存在する（二重クリック・自動リトライ）。
                // 新規作成はせず、既存の配送をそのまま返す
                // （DeliveryController.CreateDeliveryのAddIfNotExistsAsyncと同じ挙動）。
                return new DeliveryResult
                {
                    Success = true,
                    Message = "Delivery created",
                    Delivery = existingInfo
                };
            }

            // **回る順番はここで振る。** クライアントに番号を送らせると、重複や欠番を
            // そのまま受け取ることになる（本家 DeliveryController.CreateDelivery と同じ方針）。
            // 並びは受け取った配列の順。
            var info = new DeliveryInfo
            {
                OrderId = request.OrderId,
                SenderName = request.SenderName,
                SenderAddress = request.SenderAddress,
                RequestedDeliveryDate = request.RequestedDeliveryDate,
                CarrierId = request.CarrierId,
                Notes = request.Notes,
                Destinations = request.Destinations.Select((d, index) => new DeliveryDestinationInfo
                {
                    SequenceNumber = index + 1,
                    RecipientName = d.RecipientName,
                    RecipientPhone = d.RecipientPhone,
                    Address = d.Address,
                    Notes = d.Notes,
                    Packages = d.Packages,
                }).ToList(),
            };

            _store[info.Id] = info;
            _idempotencyIndex[idempotencyKey] = info.Id;

            return new DeliveryResult
            {
                Success = true,
                Message = "Delivery created",
                Delivery = info
            };
        }
    }

    public DeliveryInfo? GetDelivery(string id)
    {
        return Guid.TryParse(id, out var guid) && _store.TryGetValue(guid, out var info)
            ? info
            : null;
    }

    // 監査で発覚：**検証していたのは文字列項目のうち3つだけだった。**
    // 直下の Address / RecipientName / Notes は制御文字を弾いていたのに、
    // すぐ隣の RecipientPhone と、入れ子のDTOの中の文字列は素通りだった。
    //
    // このサンプルの狙いは「入力検証という型を示すこと」なので、一部だけ守って
    // あると **読んだ人はそれを網羅だと受け取る。** 送り状にもログにも出る値で、
    // 弾く対象になる理由は Address とまったく同じ。
    //
    // **項目の並びは本家に合わせてある**（便に1つの OrderId / SenderName /
    // SenderAddress と、届け先ごとの RecipientName / RecipientPhone / Address）。
    // 届け先が空の配列である場合も弾く——本家は
    // 「届け先を1つ以上指定してください。」（field: destinations）を返す。
    // **項目名は本家と同じ `destinations[i].xxx` の形**にする。サンプルを見て
    // 画面を作る人は、この名前でどの欄を赤くするかを決めるため。
    private static void Validate(CreateDeliveryRequest request)
    {
        RequireNonEmpty(request.OrderId, nameof(request.OrderId));
        RequireNonEmpty(request.SenderName, nameof(request.SenderName));
        RequireNonEmpty(request.SenderAddress, nameof(request.SenderAddress));

        RejectControlCharacters(request.OrderId, nameof(request.OrderId));
        RejectControlCharacters(request.SenderName, nameof(request.SenderName));
        RejectControlCharacters(request.SenderAddress, nameof(request.SenderAddress));
        RejectControlCharacters(request.Notes, nameof(request.Notes));

        // **配送業者の識別子は本家で必須。** このSDKは業者への登録を行わないが、
        // 契約として要る項目なので、ここでも弾いておく（本家 CarrierClientFactory.
        // GetClient は空文字・未対応の値を「対応していないキャリアです。」で断る）。
        // 対応している識別子を書き並べているのは、サンプルを読んだ人が
        // **何を送ればよいのか分かるようにするため。**
        RequireNonEmpty(request.CarrierId, nameof(request.CarrierId));
        if (!SupportedCarrierIds.Contains(request.CarrierId.ToLowerInvariant()))
            throw new DeliveryValidationException(
                nameof(request.CarrierId),
                $"must be one of: {string.Join(", ", SupportedCarrierIds)}.");

        // Destinations は `new()` が既定値なので、丸ごと省略しても null にはならない。
        // 検証が無いと **届け先が1軒も無いまま「作成に成功しました」が返る。**
        if (request.Destinations.Count == 0)
            throw new DeliveryValidationException("Destinations", "must contain at least one destination.");

        for (var i = 0; i < request.Destinations.Count; i++)
        {
            var destination = request.Destinations[i];
            var at = $"Destinations[{i}]";

            RequireNonEmpty(destination.RecipientName, $"{at}.RecipientName");
            RequireNonEmpty(destination.RecipientPhone, $"{at}.RecipientPhone");
            RequireNonEmpty(destination.Address, $"{at}.Address");

            RejectControlCharacters(destination.RecipientName, $"{at}.RecipientName");
            RejectControlCharacters(destination.RecipientPhone, $"{at}.RecipientPhone");
            RejectControlCharacters(destination.Address, $"{at}.Address");
            RejectControlCharacters(destination.Notes, $"{at}.Notes");

            // 本家は「荷物を1つ以上指定してください。」
            // （field: destinations[i].packages）を返す。1軒に複数口あってよい。
            if (destination.Packages.Count == 0)
                throw new DeliveryValidationException($"{at}.Packages", "must contain at least one package.");

            for (var p = 0; p < destination.Packages.Count; p++)
                RejectControlCharacters(destination.Packages[p].Description, $"{at}.Packages[{p}].Description");
        }

        if (request.IdempotencyKey is null || request.IdempotencyKey == Guid.Empty)
            throw new DeliveryValidationException(
                nameof(request.IdempotencyKey),
                "must be specified (a client-generated Guid, one per user-initiated create action).");
    }

    /// <summary>
    /// 本家が対応している配送業者の識別子。
    /// **本家の CarrierNames / 各 ICarrierClient の CarrierId と揃えること。**
    /// </summary>
    private static readonly string[] SupportedCarrierIds = ["yamato", "sagawa", "japanpost"];

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
