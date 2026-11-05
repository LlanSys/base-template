using System.IdentityModel.Tokens.Jwt;

using BT.Infrastructure.Configuration;
using BT.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

namespace BT.Tests.Unit.Infrastructure;

public sealed class JwtTimeProviderTests
{
    [Fact]
    public async Task Issuance_and_lifetime_validation_share_the_injected_clock()
    {
        var now = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        var settings = Options.Create(new JwtSettings
        {
            Secret = new string('t', 64),
            Issuer = "clock-test",
            Audience = "clock-test",
            AccessTokenExpiryMinutes = 10,
            ClockSkew = 0
        });

        var service = new JwtService(settings, NullLogger<JwtService>.Instance, clock);
        var encoded = await service.CreateTokenAsync([]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encoded);
        Assert.Equal(now.UtcDateTime, token.ValidFrom);
        Assert.Equal(now.AddMinutes(10).UtcDateTime, token.ValidTo);
        Assert.NotNull(service.GetPrincipalFromToken(encoded, validateLifetime: true));
        Assert.True(service.IsTokenValid(encoded));
        clock.GetUtcNow().Returns(now.AddMinutes(10).AddSeconds(1));
        Assert.Throws<SecurityTokenExpiredException>(() => service.GetPrincipalFromToken(encoded, validateLifetime: true));
        Assert.Throws<SecurityTokenExpiredException>(() => service.IsTokenValid(encoded));
        Assert.NotNull(service.GetPrincipalFromToken(encoded, validateLifetime: false));
    }
}
