using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SoleStockSolutions.Models
{
    public class CustomIdentity : IIdentity
    {
        public CustomIdentity(string name, string[] roles)
        {
            Name = name;
            Roles = roles;
        }

        public string Name { get; private set; }
        public string AuthenticationType { get { return "Custom"; } }
        public bool IsAuthenticated { get { return true; } }
        public string[] Roles { get; private set; }
    }
}