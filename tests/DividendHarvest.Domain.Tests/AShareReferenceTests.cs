using DividendHarvest.Domain.Securities;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class AShareReferenceTests
{
    [Fact]
    public void Create_normalizes_a_valid_reference()
    {
        var reference = AShareReference.Create(" 000001 ", "szse");

        Assert.Equal("000001", reference.SecurityCode);
        Assert.Equal("SZSE", reference.ExchangeCode);
    }

    [Theory]
    [InlineData("12345", "SSE")]
    [InlineData("1234567", "SSE")]
    [InlineData("ABCDEF", "SSE")]
    [InlineData("000001", "HKEX")]
    public void Create_rejects_non_a_share_references(string code, string exchange)
    {
        Assert.Throws<ArgumentException>(() => AShareReference.Create(code, exchange));
    }
}
