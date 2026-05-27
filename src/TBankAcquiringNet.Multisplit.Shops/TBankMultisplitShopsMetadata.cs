using System.Net;

namespace TBankAcquiringNet.Multisplit.Shops;

/// <summary>
/// HTTP-метаданные ответа API регистрации точек T-Bank Multisplit.
/// </summary>
public sealed record TBankMultisplitShopsResponseMetadata(
    HttpStatusCode HttpStatusCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    string? RawResponseBody);
