using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController(IAuthRepository authRepo, IConfiguration config, IMapper mapper)
        {
            this._mapper = mapper;
            this._authRepo = authRepo;
            this._config = config;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserForRegisterDTO userForRegisterDTO)
        {
            //Validate Request
            //if(!ModelState.IsValid)
            //    return BadRequest(ModelState);

            userForRegisterDTO.UserName = userForRegisterDTO.UserName.ToLower();
            if (await _authRepo.UserExists(userForRegisterDTO.UserName))
                return BadRequest("Username already exist");

            var userToCreate = _mapper.Map<User>(userForRegisterDTO);

            var createdUser = await _authRepo.Register(userToCreate, userForRegisterDTO.Password);

            var userToReturn = _mapper.Map<UserForDetailedDTO>(createdUser);

            return CreatedAtRoute("GetUser", new { Controller = "Users", id = createdUser.Id }, userToReturn);
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(UserForLoginDTO userForLoginDTO)
        {
            var userFromRepo = await _authRepo.Login(userForLoginDTO.UserName.ToLower(), userForLoginDTO.Password);

            if (userFromRepo == null)
                return Unauthorized();

            var claims = new []
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.UserName)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var user = _mapper.Map<UserForListDTO>(userFromRepo);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return Ok(new { token = tokenHandler.WriteToken(token), user });
        }
    }
}
