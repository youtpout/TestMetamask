using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public TokenController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Json("Api ok");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] LoginVM login)
        {
            var user = await Authenticate2(login);

            if (user != null)
            {
                var tokenString = BuildToken(user);
                return Ok(new { token = tokenString });
            }

            return Unauthorized();
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
            // TODO: this method will authenticate the user recovering the Ethereum address from signature using the Geth RPC web3.personal.ecrecover API

            UserVM user = user = new UserVM { Account = login.Signer, Name = string.Empty, Email = string.Empty };

            return user;
        }

        private async Task<UserVM> Authenticate2(LoginVM login)
        {
            // TODO: This method will authenticate the user recovering his Ethereum address through underlaying offline ecrecover method.

            UserVM user = null;

            var signer = new EthereumMessageSigner();
            var account = signer.EncodeUTF8AndEcRecover(login.Message, login.Signature);

            if (account.ToLower().Equals(login.Signer.ToLower()))
            {
                // read user from DB or create a new one
                // for now we fake a new user
                user = new UserVM { Account = account, Name = string.Empty, Email = string.Empty };
            }

            return user;
        }
    }
}
