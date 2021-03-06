﻿using System;
using System.Collections.Generic;

namespace PictureLibrary_API.Model
{
    public class Library
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Tag> Tags { get; set; }
        public List<ImageFile> Images { get; set; }
        public List<Guid> Owners { get; set; }

        public Library()
        {
            Tags = new List<Tag>();
            Images = new List<ImageFile>();
        }

        public Library(string fullPath, string name, string decription, List<Guid> owners)
        {
            FullName = fullPath;
            Name = name;
            Description = decription;
            Owners = owners;
            Tags = new List<Tag>();
            Images = new List<ImageFile>();
        }

        public Library(string fullPath, string name, string description, List<Guid> owners, List<Tag> tags, List<ImageFile> images)
        {
            FullName = fullPath;
            Name = name;
            Description = description;
            Owners = owners;
            Tags = tags;
            Images = images;
        }
    }
}
