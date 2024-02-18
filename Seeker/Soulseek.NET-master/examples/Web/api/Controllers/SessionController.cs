﻿namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using WebAPI.DTO;

    /// <summary>
    ///     Session
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class SessionController : ControllerBase
    {
        public SessionController()
        {
        }

        /// <summary>
        ///     Checks whether security is enabled.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">True if security is enabled, false otherwise.</response>
        [HttpGet]
        [Route("enabled")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(bool), 200)]
        public IActionResult Enabled()
        {
            return Ok(Startup.EnableSecurity);
        }

        /// <summary>
        ///     Checks whether the provided authentication is valid.
        /// </summary>
        /// <remarks>
        ///     This is a no-op provided so that the application can test for an expired token on load.
        /// </remarks>
        /// <returns></returns>
        /// <response code="200">The authentication is valid.</response>
        /// <response code="403">The authentication is is invalid.</response>
        [HttpGet]
        [Route("")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public IActionResult Check()
        {
            return Ok();
        }

        /// <summary>
        ///     Logs in.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        /// <response code="200">Login was successful.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Login failed.</response>
        [HttpPost]
        [Route("")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult Login([FromBody]LoginRequest login)
        {
            if (login == default)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest("Username and/or Password missing or invalid");
            }

            if (login.Username == Startup.Username && login.Password == Startup.Password)
            {
                return Ok(new TokenResponse(GetJwtSecurityToken()));
            }

            return Unauthorized();
        }

        private static JwtSecurityToken GetJwtSecurityToken()
        {
            var issuedUtc = DateTime.UtcNow;
            var expiresUtc = DateTime.UtcNow.AddMilliseconds(Startup.SecurityTokenTTL);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, Startup.Username),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("name", Startup.Username),
                new Claim("iat", ((DateTimeOffset)issuedUtc).ToUnixTimeSeconds().ToString())
            };

            var credentials = new SigningCredentials(Startup.JwtSigningKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "slsk-web-example",
                claims: claims,
                notBefore: issuedUtc,
                expires: expiresUtc,
                signingCredentials: credentials);

            return token;
        }
    }
}
