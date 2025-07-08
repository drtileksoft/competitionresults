using System;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CompetitionResults.Data
{
    public class UserIdStateService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private string _userId;

        public UserIdStateService(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<string> GetUserIdAsync()
        {
            if (string.IsNullOrEmpty(_userId))
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            return _userId;
        }
    }
}



