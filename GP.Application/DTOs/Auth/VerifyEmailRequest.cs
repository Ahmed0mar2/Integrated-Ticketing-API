using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record VerifyEmailRequest
    {
        public string UserId { get; init; } = null!;
        public string Token { get; init; } = null!;
    }
}
