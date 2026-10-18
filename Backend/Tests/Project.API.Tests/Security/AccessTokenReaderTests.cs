using Project.API.Security;

namespace Project.API.Tests.Security;

public class AccessTokenReaderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("Bearer \"\"")]
    public void TryRead_ReturnsFalse_WhenTheHeaderHasNoToken(string? authorization)
    {
        AccessTokenReader.TryRead(authorization, out var token).Should().BeFalse();
        token.Should().BeEmpty();
    }

    [Theory]
    [InlineData("eyJ.payload.sig")]
    [InlineData("Bearer eyJ.payload.sig")]
    [InlineData("bearer eyJ.payload.sig")]
    [InlineData("Bearer Bearer eyJ.payload.sig")]
    [InlineData("Bearer \"eyJ.payload.sig\"")]
    [InlineData("  Bearer   \"eyJ.payload.sig\"  ")]
    public void TryRead_ReturnsTheJwt_WhenSwaggerRepeatsTheBearerPrefixOrQuotes(string authorization)
    {
        AccessTokenReader.TryRead(authorization, out var token).Should().BeTrue();
        token.Should().Be("eyJ.payload.sig");
    }
}
