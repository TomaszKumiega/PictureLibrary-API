﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PictureLibrary_API.Helpers;
using PictureLibrary_API.Model;
using PictureLibrary_API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PictureLibrary_API.Tests.ServicesTests
{
    public class UserServiceTests
    {
        #region Private helper methods
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using(var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
        #endregion

        #region Authenticate 
        [Fact]
        public void Authenticate_ShouldReturnUser_WhenUsernameAndPasswordAreValid()
        {
            var contextMock = new Mock<IDatabaseContext>();
            var loggerMock = new Mock<ILogger<UserService>>();

            var username = "testUser";
            var password = "passw";

            byte[] passwordHash;
            byte[] passwordSalt;

            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            var user =
                new User()
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = "randomEmail@example.com",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

            var dbSet = new TestDbSet<User>();
            dbSet.Add(user);

            contextMock.Setup(x => x.Users)
                .Returns(dbSet);

            var userService = new UserService(loggerMock.Object, contextMock.Object);

            var result = userService.Authenticate(username, password);

            Assert.True(result.Id == user.Id);            
        }

        [Fact]
        public void Authenticate_ShouldThrowArgumentException_WhenUsernameOrPasswordAreNullOrEmpty()
        {
            var contextMock = new Mock<IDatabaseContext>();
            var loggerMock = new Mock<ILogger<UserService>>();

            var userService = new UserService(loggerMock.Object, contextMock.Object);

            Assert.Throws<ArgumentException>(() => userService.Authenticate(null, "gdagadgd"));
            Assert.Throws<ArgumentException>(() => userService.Authenticate("gadgag", null));
            Assert.Throws<ArgumentException>(() => userService.Authenticate(String.Empty, "gdagdag"));
            Assert.Throws<ArgumentException>(() => userService.Authenticate("gadgag", String.Empty));
        }
        #endregion
    }
}