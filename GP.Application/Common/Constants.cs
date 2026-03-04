using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Common
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Partner = "Partner";
    }

    public static class Policies
    {
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireUserRole = "RequireUserRole";
        public const string RequirePartnerRole = "RequirePartnerRole";
    }
}
