using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record LoginRequest
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string? DeviceInfo { get; init; }
    }
}
