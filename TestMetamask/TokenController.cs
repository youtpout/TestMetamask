using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Signer;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace TestMetamask
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : Controller
    {
        private IConfiguration _config;
        private readonly IMemoryCache _cache;
        private const string message = "Welcome to the Heroverse, sign this message to authenticate {0}";
        private const string cacheConnectionKey = "GetConnection_{0}";

        public TokenController(IConfiguration config, IMemoryCache cache)
        {
            _config = config;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Json("Api ok");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> RequestConnection(string account)
        {
            var connectVm = RequestEntry(account);

            return Json(new MessageVM { Account = connectVm.Account, Message = string.Format(message, connectVm.Nonce) });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] LoginVM login)
        {
            var user = await Authenticate(login);

            if (user != null)
            {
                var tokenString = BuildToken(user);
                return Ok(new { token = tokenString });
            }

            return Unauthorized();
        }

        private ConnectionVM CheckEntry(string account)
        {
            return GetCacheConnection(account);
        }

        private ConnectionVM RequestEntry(string account)
        {
            return GetCacheConnection(account);
        }

        private ConnectionVM GetCacheConnection(string account)
        {
            var key = string.Format(cacheConnectionKey, account);
            return _cache.GetOrCreate(cacheConnectionKey, (entry) =>
             {
                 entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                 var test = new ConnectionVM();
                 return new ConnectionVM { Account = account, DateTime = DateTime.Now, Nonce = new Guid() };
             });
        }

        private void ResetCache(string account)
        {
            var key = string.Format(cacheConnectionKey, account);
            _cache.Remove(cacheConnectionKey);
        }

        private string BuildToken(UserVM user)
        {
            var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Account),
            new Claim(JwtRegisteredClaimNames.GivenName, user.Name),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Audience"],
              claims,
              expires: DateTime.Now.AddMinutes(30),
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserVM> Authenticate(LoginVM login)
        {
            // TODO: This method will authenticate the user recovering his Ethereum address through underlaying offline ecrecover method.

            var connectVM = CheckEntry(login.Signer);
            var messageVM = string.Format(message, connectVM.Nonce);

            // delete from cache to revoke reuse
            ResetCache(login.Signer);

            if (messageVM != login.Message)
            {
                _cache.Remove(cacheConnectionKey);
                throw new Exception("Authentification expired retry");
            }


            var messageToVerify = string.Format(message, connectVM.Nonce);
            UserVM user = null;

            var signer = new EthereumMessageSigner();
            var account = signer.EncodeUTF8AndEcRecover(messageToVerify, login.Signature);

            if (account.ToLower().Equals(login.Signer.ToLower()))
            {
                // read user from DB or create a new one
                // for now we fake a new user
                user = new UserVM { Account = account, Name = string.Empty, Email = string.Empty };
            }
            else
            {
                throw new Exception("Impossible to valid your auth, signature incorrect");
            }

            return user;
        }
    }
}
