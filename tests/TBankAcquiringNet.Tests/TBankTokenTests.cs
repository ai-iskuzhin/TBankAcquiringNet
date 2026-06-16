using TBankAcquiringNet;

namespace TBankAcquiringNet.Tests;

public sealed class TBankTokenTests
{
    [Fact]
    public void Create_UsesDocumentedOrderingAndSha256()
    {
        var request = new TBankPaymentStateRequest
        {
            TerminalKey = "TestB",
            PaymentId = "20150"
        };

        var token = TBankToken.Create(request, "Dfsfh56dgKl");

        Assert.Equal("03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9", token);
    }

    [Fact]
    public void Create_ExcludesExistingToken()
    {
        var unsigned = new TBankPaymentStateRequest
        {
            TerminalKey = "TestB",
            PaymentId = "20150"
        };

        var signed = unsigned with
        {
            Token = "already-present"
        };

        Assert.Equal(
            TBankToken.Create(unsigned, "Dfsfh56dgKl"),
            TBankToken.Create(signed, "Dfsfh56dgKl"));
    }

    [Fact]
    public void Create_IgnoresNestedDataObjectForGenericToken()
    {
        var withoutData = new TBankInitPaymentRequest
        {
            TerminalKey = "TerminalKey",
            Amount = TBankAmount.FromMinorUnits(15000),
            OrderId = "sp123"
        };

        var withData = withoutData with
        {
            DATA = new Dictionary<string, string?>
            {
                ["Email"] = "customer@example.test",
                ["Phone"] = "+70000000000"
            }
        };

        Assert.Equal(
            TBankToken.Create(withoutData, "secret"),
            TBankToken.Create(withData, "secret"));
    }

    [Fact]
    public void Verify_ComparesExpectedToken()
    {
        var request = new TBankPaymentStateRequest
        {
            TerminalKey = "TestB",
            PaymentId = "20150"
        };

        Assert.True(TBankToken.Verify(
            request,
            "Dfsfh56dgKl",
            "03acc0a77d6e870f402a1038c1ca5d8b4a985fe76f08016a869f10f2382bd7a9"));
    }

    [Fact]
    public void Create_FormatsQrDataTypeAsWireValue()
    {
        var request = new TBankQrRequest
        {
            TerminalKey = "TestB",
            PaymentId = "20150",
            DataType = TBankQrDataType.Image
        };

        var token = TBankToken.Create(request, "Dfsfh56dgKl");

        Assert.Equal("a65eecdd9fc78ae498777ce3a89462c5f5cdb996511aa3076f0e9e1c42d8b5d9", token);
    }
}
