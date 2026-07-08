# Testing

Run the regular unit test suite:

```bash
dotnet test tests/TBankAcquiringNet.Tests/TBankAcquiringNet.Tests.csproj
```

Run all tests in the solution:

```bash
dotnet test TBankAcquiringNet.slnx
```

## Payments Integration Tests

Payments integration tests live in a separate project:

```text
tests/TBankAcquiringNet.Tests.Integration
```

They do not store credentials in the repository. They run real HTTP calls only when the required environment variables are present.

For `TBankAcquiringNet`:

```bash
export TBANK_ACQUIRING_TEST_TERMINAL_KEY="..."
export TBANK_ACQUIRING_TEST_PASSWORD="..."
export TBANK_ACQUIRING_TEST_BASE_URL="https://securepay.tinkoff.ru/v2/"
dotnet test tests/TBankAcquiringNet.Tests.Integration/TBankAcquiringNet.Tests.Integration.csproj
```

Or create a local `.env` from the committed template:

```bash
cp .env.example .env
```

Fill in the values, then load them before running integration tests:

```bash
set -a
source .env
set +a
dotnet test tests/TBankAcquiringNet.Tests.Integration/TBankAcquiringNet.Tests.Integration.csproj
```

The integration test creates a fresh payment session with `Init`, reconciles it with `CheckOrder`, calls `GetState`, cancels the payment, and calls `GetState` again. `PaymentId` is returned by `Init` at runtime and should not be stored as configuration.

The payment API uses `TerminalKey` and terminal `Password` for request token generation. Other credential types (such as OAuth login/password used by multisplit registration APIs) are out of scope for this package.
