using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = null!;
    }
}
