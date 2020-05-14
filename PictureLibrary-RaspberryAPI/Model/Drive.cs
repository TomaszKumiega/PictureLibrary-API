﻿using System;
using PictureLibraryModel.Services;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PictureLibrary_RaspberryAPI.Controllers;

namespace PictureLibraryModel.Model
{
    public class Drive : IFileSystemEntity
    {
        private readonly ILogger<Drive> _logger;

        private bool _isExpanded;
        private IFileSystemService FileSystemService { get; }

        public string Name { get; }
        public string FullPath { get; }
        public string ImageSource { get; }
        public ObservableCollection<object> Children { get; set; }
        

        public Drive(string name, IFileSystemService fileSystemService)
        {
            Name = name;
            FullPath = name;
            this.Children = new ObservableCollection<object>();
            this.FileSystemService = fileSystemService;

            ImageSource = "pack://application:,,,/Icons/DiskIcon.png";
            Initialize();
        }

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }

            set
            {
                _isExpanded = value;
                LoadChildrenDirectories();
            }
        }

        private async Task Initialize()
        {
            try
            {
                var directories = await Task.Run(() =>
                    FileSystemService.GetAllDirectories(Name, System.IO.SearchOption.TopDirectoryOnly));


                if (directories != null)
                {
                    foreach (var t in directories)
                    {
                        Children.Add(t);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
            }
        }
        

        private async Task LoadChildrenDirectories()
        {
            foreach (Directory c in Children)
            {
                var directories = await Task.Run(() => FileSystemService.GetAllDirectories(c.FullPath, System.IO.SearchOption.TopDirectoryOnly));

                if (directories != null)
                {
                    foreach (var t in directories)
                    {
                        c.Children.Add(t);
                    }    
                }
            }
        }
    }
}
