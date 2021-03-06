﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PictureLibrary_API.Model
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key()]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; }
    }
}
