﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PictureLibrary_API.Exceptions;
using PictureLibrary_API.Helpers;
using PictureLibrary_API.Model;
using PictureLibrary_API.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PictureLibrary_API.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private ILogger<UsersController> Logger { get; }
        private IUserService UserService { get; }
        private IMapper Mapper { get; }
        private IAccessTokenService AccessTokenService { get; }

        public UsersController(ILogger<UsersController> logger, IMapper mapper, IUserService userService, IAccessTokenService refreshTokenService)
        {
            Logger = logger;
            Mapper = mapper;
            UserService = userService;
            AccessTokenService = refreshTokenService;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody]AuthenticateModel model)
        {
            User user = null;
            string tokenString;
            string refreshToken;

            try
            {
                user = await Task.Run(() => UserService.Authenticate(model.Username, model.Password));

                if (user == null)
                {
                    return BadRequest(new { message = "Username or password is incorrect" });
                }

                tokenString = AccessTokenService.GenerateAccessToken(user.Id.ToString());
                refreshToken = AccessTokenService.GenerateRefreshToken();
                AccessTokenService.SaveRefreshToken(user.Id.ToString(), refreshToken);
            }
            catch(ArgumentException)
            {
                return BadRequest();
            }
            catch
            {
                return StatusCode(500);
            }
            
            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                token = tokenString,
                refreshToken = refreshToken
            });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserModel model)
        {
            var user = Mapper.Map<User>(model);
            User result = null;

            try
            {
                result = await Task.Run(() => UserService.Create(user, model.Password));
            }
            catch (ArgumentException e)
            {
                return BadRequest(new { message = e.Message });
            }
            catch (UserAlreadyExistsException e)
            {
                return Conflict(new { message = e.Message });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }

            return Created("", new
            {
                id = result.Id,
                username = result.Username,
                email = result.Email
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestModel refreshRequest)
        {
            var principal = await Task.Run(() =>  AccessTokenService.GetPrincipalFromExpiredToken(refreshRequest.Token));
            var userId = principal.Identity.Name;
            var savedRefreshToken =await Task.Run(() =>  AccessTokenService.GetRefreshToken(userId)); 
            if (savedRefreshToken != refreshRequest.RefreshToken)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            string newJwtToken = null;
            string newRefreshToken = null;

            try
            {
                newJwtToken = await Task.Run(() => AccessTokenService.GenerateAccessToken(userId));
                newRefreshToken = await Task.Run(() => AccessTokenService.GenerateRefreshToken());

                await Task.Run(() => AccessTokenService.DeleteRefreshToken(userId, refreshRequest.RefreshToken));
                await Task.Run(() => AccessTokenService.SaveRefreshToken(userId, newRefreshToken));
            }
            catch(Exception e)
            {
                Logger.LogError(e, e.Message);
                return StatusCode(500);
            }

            return Ok( new
            {
                token = newJwtToken,
                refreshToken = newRefreshToken
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody]UserModel model)
        {
            var userId = User?.Identity.Name;
            if (userId != id.ToString())
            {
                return Unauthorized();
            }

            var user = Mapper.Map<User>(model);
            user.Id = id;

            try
            { 
                await Task.Run(() => UserService.Update(user, model.Password));
            }
            catch(ContentNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch(UserAlreadyExistsException e)
            {
                return Conflict(new { message = e.Message });
            }
            catch(Exception e)
            {
                Logger.LogError(e, e.Message);
                return StatusCode(500);
            }

            return Ok();
        }
    }
}