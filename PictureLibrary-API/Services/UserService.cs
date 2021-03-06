﻿using Microsoft.Extensions.Logging;
using PictureLibrary_API.Exceptions;
using PictureLibrary_API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureLibrary_API.Services
{
    public class UserService : IUserService
    {
        private ILogger<UserService> Logger { get; }
        private IDatabaseContext DatabaseContext { get; }

        public UserService(ILogger<UserService> logger, IDatabaseContext databaseContext)
        {
            Logger = logger;
            DatabaseContext = databaseContext;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Value cannot be empty or null", "username");
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Value cannot be empty or null", "password");

            var user = await Task.Run(() => DatabaseContext.Users.SingleOrDefault(x => x.Username == username));

            if (user == null) return null;
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            } 

            return user;
        }

        public async Task<User> CreateAsync(UserModel userModel)
        {
            if (userModel == null) throw new ArgumentNullException("Value cannot be null", "userModel");
            if (string.IsNullOrEmpty(userModel.Username)) throw new ArgumentException("Value cannot be empty or null", "username");
            if (string.IsNullOrEmpty(userModel.Password)) throw new ArgumentException("Value cannot be empty or null", "password");


            if (DatabaseContext.Users.Any(x => x.Username == userModel.Username))
            {
                throw new UserAlreadyExistsException("Username: \"" + userModel.Username + "\" is already taken");
            }

            User user = new User();

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(userModel.Password, out passwordHash, out passwordSalt);

            user.Id = Guid.NewGuid();
            user.Username = userModel.Username;
            user.Email = userModel.Email;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await Task.Run(() => DatabaseContext.Users.Add(user));
            await Task.Run(() => DatabaseContext.SaveChanges());

            Logger.LogInformation("New user added: " + user.Id.ToString());

            return user;
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await Task.Run(() => DatabaseContext.Users.FirstOrDefault(x => x.Id == id));
            if (user != null)
            {
                DatabaseContext.Users.Remove(user);
                DatabaseContext.SaveChanges();

                Logger.LogInformation("User removed: " + user.Id.ToString());
            }
            else
            {
                throw new ContentNotFoundException("User " + id.ToString() + " not found.");
            }
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await Task.Run(() => DatabaseContext.Users);
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            return await Task.Run(() => DatabaseContext.Users.FirstOrDefault(x => x.Id == id));
        }

        public async Task<User> FindAsync(Func<User, bool> predicate)
        {
            return await Task.Run(() => DatabaseContext.Users.FirstOrDefault(predicate));
        }

        public async Task UpdateAsync(User userUpdateInfo, string password = null)
        {
            if(string.IsNullOrEmpty(userUpdateInfo.Username) && string.IsNullOrEmpty(userUpdateInfo.Email) && string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("At least one of the updated properties is expected to have value");
            }

            var user = await Task.Run(() => DatabaseContext.Users.FirstOrDefault(x => x.Id == userUpdateInfo.Id));

            if (user == null)
            {
                throw new ContentNotFoundException("User not found");
            }

            if (!string.IsNullOrEmpty(userUpdateInfo.Username) && userUpdateInfo.Username != user.Username)
            { 
                if (DatabaseContext.Users.Any(x => x.Username == userUpdateInfo.Username))
                {
                    throw new UserAlreadyExistsException("Username \"" + userUpdateInfo.Username + "\" is already taken");
                }
                    
                user.Username = userUpdateInfo.Username;
            }

            if(!string.IsNullOrEmpty(userUpdateInfo.Email))
            {
                user.Email = userUpdateInfo.Email;
            }

            if (!string.IsNullOrEmpty(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            await Task.Run(() => DatabaseContext.Users.Update(user));
            await Task.Run(() => DatabaseContext.SaveChanges());

            Logger.LogInformation("Updated user: " + user.Id);
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }
    }
}
