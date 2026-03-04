using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Auth
{
    public record ChangePasswordRequest
    {
        public string CurrentPassword { get; init; } = null!;
        public string NewPassword { get; init; } = null!;
        public string ConfirmPassword { get; init; } = null!;
    }
}
