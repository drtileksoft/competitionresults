using CompetitionResults.Data;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Xunit;

namespace CompetitionResults.Tests;

public class StateServiceTests
{
    [Fact]
    public async Task UserIdStateService_ReturnsUserId()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user123") }, "test");
        var principal = new ClaimsPrincipal(identity);
        var provider = new TestAuthStateProvider(principal);
        var service = new UserIdStateService(provider);

        var id = await service.GetUserIdAsync();
        Assert.Equal("user123", id);
    }

    [Fact]
    public void CompetitionStateService_RaisesEvent()
    {
        var service = new CompetitionStateService();
        bool called = false;
        service.OnCompetitionChanged += () => called = true;

        service.SelectedCompetitionId = 10;

        Assert.True(called);
        Assert.Equal(10, service.SelectedCompetitionId);
    }
}

internal class TestAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state;
    public TestAuthStateProvider(ClaimsPrincipal principal)
    {
        _state = new AuthenticationState(principal);
    }
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
}
