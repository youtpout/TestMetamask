using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestMetamask
{
    public class LoginVM
    {
        public string Signer { get; set; } // Ethereum account that claim the signature
        public string Signature { get; set; } // The signature
        public string Message { get; set; } // The plain message
        public string Hash { get; set; } // The prefixed and sha3 hashed message 
    }

    public class UserVM
    {
        public string Account { get; set; } // Unique account name (the Ethereum account)
        public string Name { get; set; } // The user name
        public string Email { get; set; } // The user Email
    }
}
