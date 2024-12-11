using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SoleStockSolutions.Models
{
    public class CustomPrincipal : IPrincipal
    {
        private CustomIdentity _identity;

        public CustomPrincipal(CustomIdentity identity)
        {
            _identity = identity;
        }

        public IIdentity Identity
        {
            get { return _identity; }
        }

        public bool IsInRole(string role)
        {
            return _identity.Roles.Contains(role);
        }
    }
}