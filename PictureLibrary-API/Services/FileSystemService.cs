﻿using Castle.Core.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using PictureLibrary_API.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureLibraryModel.Services
{
    public class FileSystemService : IFileSystemService
    {
        private readonly ILogger<FileSystemService> _logger;
        private string TargetDirectory { get; set; }

        public FileSystemService(ILogger<FileSystemService> logger)
        {
            _logger = logger;
            InitializeTargetDirectory();
        }

        private void InitializeTargetDirectory()
        {
            if (Directory.Exists("PictureLibraryFileSystem/"))
            {
                TargetDirectory = "PictureLibraryFileSystem/";
            }
            else
            {
                throw new Exception("Target directory not found");
            }
        }


        public FileStream CreateFile(string filePath)
        {
            if (filePath.IsNullOrEmpty()) throw new ArgumentException();

            try
            {
                var fileStream = File.Create(TargetDirectory + filePath);
                return fileStream;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, e.Message);
            }
            
            throw new Exception("Operation failed");
        }

        public string AddFile(string filePath, byte[] file)
        {
            if (filePath.IsNullOrEmpty()) throw new ArgumentException();

            try
            {
                File.WriteAllBytes(TargetDirectory + filePath, file);

                return TargetDirectory + filePath;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, e.Message);
            }
           
            throw new Exception("Operation failed");
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        public List<string> FindFiles(string searchPattern, string directory)
        {
            var fileStreams = new List<FileStream>();

            var files = Directory.GetFiles(TargetDirectory + "\\" + directory, searchPattern, SearchOption.AllDirectories).ToList();

            return files;
        }

        public string? GetExtension(string path)
        {
           return Path.GetExtension(path);
        }

        public byte[] GetFile(string path)
        {
            return File.ReadAllBytes(path);
        }

        public FileStream OpenFile(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }

        public void RenameFile(string file, string newName)
        {
            FileSystem.RenameFile(file, newName);
        }

        public FileInfo GetFileInfo(string path)
        {
            return new FileInfo(path);
        }
    }
}
