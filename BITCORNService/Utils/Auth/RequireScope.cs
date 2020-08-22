using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace BITCORNService.Utils.Auth
{
    public class RequireScope : IAuthorizationRequirement
    {
        public string Issuer { get; }
        public string Scope { get; }

        public RequireScope(string scope, string issuer)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
    }
}
