using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record RevokeTokenRequest
    {
        public string RefreshToken { get; init; } = null!;
    }
}
