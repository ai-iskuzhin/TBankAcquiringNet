using System.Text;
using TBankAcquiringNet;

// Sample reproducing the multisplit "DealId" problem against the production T-Bank API.
//
// Symptom seen from the field:
//   Init responded 200 with {"Success":false,"ErrorCode":"934",
//   "Message":"Неверные параметры.","Details":"Некорректный формат идентификатора сделки"}
//   while sending DealId="e190a9c7-e2fe-476f-9019-e29fe3d840b8" (a GUID) together with
//   CreateDealWithType="MULTISPLIT".
//
// Root cause (see docs/integrations/t-bank-acquiring/oplata_multisplit.md, table 2.3.1):
//   * DealId is type **Number** — a numeric identifier ISSUED BY T-Bank (e.g. 23123123),
//     not a GUID you mint yourself. A GUID fails format validation -> error 934.
//   * DealId and CreateDealWithType are mutually exclusive. CreateDealWithType is the
//     "create a new deal" flag and is only honored when there is NO DealId. To open a new
//     multisplit deal you send CreateDealWithType WITHOUT DealId; T-Bank creates the deal
//     and returns its numeric id (SpAccumulationId) in the notification / GetState.
//   * To add a payment to an EXISTING deal you send that numeric DealId and OMIT
//     CreateDealWithType.
//   * The deal type value is "NN" (safe-deal / nominal account). "MULTISPLIT" is NOT a
//     valid CreateDealWithType value and returns ErrorCode 256 ("Указан некорректный тип
//     безопасной сделки").
//
// All console output is mirrored to a timestamped log file under samples/DealIdSample/logs/.

LoadDotEnv();

// Mirror everything written to the console into a log file as well.
Console.OutputEncoding = Encoding.UTF8;
var logDirectory = Path.Combine(ResolveProjectDirectory(), "logs");
Directory.CreateDirectory(logDirectory);
var logPath = Path.Combine(logDirectory, $"dealid-sample-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}.log");
var logFile = new StreamWriter(logPath, append: false, Encoding.UTF8) { AutoFlush = true };
var originalOut = Console.Out;
using var tee = new TeeTextWriter(originalOut, logFile);
Console.SetOut(tee);

Console.WriteLine($"Log file: {logPath}");
Console.WriteLine($"Run started (UTC): {DateTimeOffset.UtcNow:O}");
Console.WriteLine();

var terminalKey = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_TERMINAL_KEY");
var password = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_PASSWORD");
var baseUrl = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_BASE_URL");

if (string.IsNullOrWhiteSpace(terminalKey) || string.IsNullOrWhiteSpace(password))
{
    Console.WriteLine(
        "Missing credentials. Set TBANK_ACQUIRING_TERMINAL_KEY and TBANK_ACQUIRING_PASSWORD " +
        "in the repo-root .env file.");
    Console.SetOut(originalOut);
    return 1;
}

using var httpClient = new HttpClient();
var client = new TBankPaymentsClient(httpClient, new TBankPaymentsClientOptions
{
    TerminalKey = terminalKey,
    Password = password,
    BaseAddress = string.IsNullOrWhiteSpace(baseUrl) ? null : new Uri(baseUrl),
    CaptureRawResponseBody = true
});

Console.WriteLine($"BaseAddress  : {baseUrl}");
Console.WriteLine($"TerminalKey  : {terminalKey}");

var recipientId = Environment.GetEnvironmentVariable("TBANK_ACQUIRING_PAYMENT_RECIPIENT_ID") ?? "2942661";

// ---------------------------------------------------------------------------
// Case 1 — reproduce the failure: a GUID passed as DealId.
// ---------------------------------------------------------------------------
// Console.WriteLine();
// Console.WriteLine("=== Case 1: GUID DealId (reproduces ErrorCode 934) ===");
// var badResponse = await client.InitAsync(new TBankInitPaymentRequest
// {
//     Amount = TBankAmount.FromMinorUnits(1000),
//     OrderId = NewOrderId(),
//     Description = "Multisplit deal sample — bad DealId",
//     PaymentRecipientId = recipientId,

//     DealId = "e190a9c7-e2fe-476f-9019-e29fe3d840b8", // GUID — wrong format for a Number field
//     CreateDealWithType = "MULTISPLIT",
//     PayType = "O"
// });
// PrintResult("Init", badResponse, badResponse.Metadata);

// ---------------------------------------------------------------------------
// Case 2 — the fix: open a new deal with CreateDealWithType and NO DealId.
// T-Bank mints the numeric deal id and returns it via notification / GetState.
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Case 2: open a new deal (CreateDealWithType, no DealId) ===");
var goodResponse = await client.InitAsync(new TBankInitPaymentRequest
{
    Amount = TBankAmount.FromMinorUnits(1000),
    OrderId = NewOrderId(),
    Description = "Multisplit deal sample — create deal",
    // PaymentRecipientId = recipientId,
    CreateDealWithType = "NN", // valid deal type ("NN"); no DealId -> T-Bank creates the deal
    PayType = "O"
});
PrintResult("Init", goodResponse, goodResponse.Metadata);

if (goodResponse.Success)
{
    Console.WriteLine();
    Console.WriteLine("PaymentURL: " + goodResponse.PaymentURL);

    // The Init response does NOT carry the deal id. Query GetState and dump the raw body
    // to see the numeric SpAccumulationId (== DealId) that T-Bank created for this deal.
    Console.WriteLine();
    Console.WriteLine("=== GetState (look for SpAccumulationId / DealId) ===");
    var state = await client.GetStateAsync(new TBankPaymentStateRequest
    {
        PaymentId = goodResponse.PaymentId!
    });
    PrintRaw("GetState", state.Metadata);

    Console.WriteLine();
    Console.WriteLine(
        $"Status is '{state.Status}'. The numeric deal id (SpAccumulationId) is NOT returned " +
        "while the payment is still NEW/unpaid. T-Bank sends it as 'SpAccumulationId' in the " +
        "POST notification once the payment is processed (and in GetState after that). " +
        "TBankPaymentNotification.SpAccumulationId surfaces it — reuse that value as DealId " +
        "(without CreateDealWithType) for further payments into the same deal.");
}

// ---------------------------------------------------------------------------
// Case 3 — the DATA-nested variant from the T-Bank "Протокол EACQ Мультирасчеты"
// Init examples (StartSpAccumulation / SpAccumulationId / BasicFieldKey / Confidant
// inside DATA). The docs present it as a second column alongside the top-level
// fields, but ON THIS PRODUCTION TERMINAL it is REJECTED with ErrorCode 937
// ("Тип сделки должен быть передан в параметре CreateDealWithType для сделки NN").
// Conclusion: use the TOP-LEVEL deal fields (Case 2), not the DATA-nested form.
//   USE: top-level CreateDealWithType="NN" (create) / DealId="<num>" (existing)
//   NOT: DATA.StartSpAccumulation / DATA.SpAccumulationId
// DATA stays for customer data (Phone, Email, custom key-values) — not deal flags.
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Case 3: DATA-nested form (StartSpAccumulation) — rejected on this terminal ===");
var dataResponse = await client.InitAsync(new TBankInitPaymentRequest
{
    Amount = TBankAmount.FromMinorUnits(1000),
    OrderId = NewOrderId(),
    Description = "Multisplit deal sample — create deal via DATA",
    PaymentRecipientId = recipientId,
    PayType = "O",
    DATA = new Dictionary<string, string?>
    {
        ["StartSpAccumulation"] = "NN" // create-new-deal flag, DATA-nested form (no SpAccumulationId)
    }
});
PrintResult("Init", dataResponse, dataResponse.Metadata);

Console.WriteLine();
Console.WriteLine($"Run finished (UTC): {DateTimeOffset.UtcNow:O}");
Console.SetOut(originalOut);
Console.WriteLine($"Full output logged to: {logPath}");
return 0;

static string NewOrderId() => $"SGH-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";

// Walks up from the build output to the directory that holds DealIdSample.csproj so logs
// land in the source tree, not under bin/. Falls back to the base directory.
static string ResolveProjectDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "DealIdSample.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return AppContext.BaseDirectory;
}

static void PrintResult(string method, TBankInitPaymentResponse response, TBankAcquiringResponseMetadata? metadata)
{
    Console.WriteLine($"Success   : {response.Success}");
    Console.WriteLine($"ErrorCode : {response.ErrorCode}");
    Console.WriteLine($"Message   : {response.Message}");
    Console.WriteLine($"Details   : {response.Details}");
    Console.WriteLine($"PaymentId : {response.PaymentId}");
    PrintRaw(method, metadata);
}

static void PrintRaw(string method, TBankAcquiringResponseMetadata? metadata)
{
    Console.WriteLine($"HTTP      : {(int?)metadata?.HttpStatusCode} ({metadata?.HttpStatusCode})");
    Console.WriteLine($"Raw {method,-8}: {metadata?.RawResponseBody}");
}

// Minimal .env loader: walks up from the working directory to find the repo-root .env
// and sets any variables that are not already present in the environment.
static void LoadDotEnv()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, ".env");
        if (File.Exists(candidate))
        {
            foreach (var line in File.ReadAllLines(candidate))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var separator = trimmed.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = trimmed[..separator].Trim();
                var value = trimmed[(separator + 1)..].Trim();
                if (Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        directory = directory.Parent;
    }
}

// Writes every character to two underlying writers (console + log file).
file sealed class TeeTextWriter(TextWriter primary, TextWriter secondary) : TextWriter
{
    public override Encoding Encoding => primary.Encoding;

    public override void Write(char value)
    {
        primary.Write(value);
        secondary.Write(value);
    }

    public override void Write(string? value)
    {
        primary.Write(value);
        secondary.Write(value);
    }

    public override void WriteLine(string? value)
    {
        primary.WriteLine(value);
        secondary.WriteLine(value);
    }

    public override void Flush()
    {
        primary.Flush();
        secondary.Flush();
    }
}
