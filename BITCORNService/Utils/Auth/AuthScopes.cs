using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Auth
{
    public class AuthScopes
    {
        public const string Withdraw = "transaction:withdraw";
        public const string Deposit = "transaction:deposit";
        public const string SendTransaction = "transaction:send";
        public const string ReadTransaction = "transaction:read";
        public const string ChangeUser = "user:change";
        public const string AddUser = "user:add";
        public const string BanUser = "user:ban";
        public const string ReadUser = "user:read";
        public const string CreateOrder = "create:order";
        public const string AuthorizeOrder = "authorize:order";
    }
}
