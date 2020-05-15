﻿using PictureLibraryModel.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PictureLibraryModel.Services
{
    public class FileSystemService : IFileSystemService
    {

        private readonly ILogger<FileSystemService> _logger;


        public FileSystemService()
        {

        }

        public void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite) 
        {
            File.Copy(sourceFilePath, destinationFilePath,overwrite);
        }

        public List<Model.Directory> GetAllDirectories(string topDirectory, SearchOption option)
        {
            if (topDirectory != null)
            {
                if (System.IO.Directory.Exists(topDirectory))
                {
                    string[] fullPaths = null;

                    try
                    {
                        fullPaths = System.IO.Directory.GetDirectories(topDirectory, "*", option);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Couldn't load directories from " + fullPaths);
                    }

                    var directories = new List<Model.Directory>();

                    if (fullPaths != null)
                    {
                        foreach (var t in fullPaths)
                        {
                            directories.Add(new Model.Directory(t, (new System.IO.DirectoryInfo(t)).Name, this));
                        }
                    }

                    return directories;
                }
                else throw new DirectoryNotFoundException("Directory: " + topDirectory + " not found");
                
            }
            else throw new ArgumentNullException();
        }

        public List<Drive> GetDrives()
        {
            var drives = new List<Drive>();
            drives.Add(new Drive("My Computer", new FileSystemService()));

            foreach(var driveInfo in System.IO.DriveInfo.GetDrives())
            {
                drives[0].Children.Add(new Drive(driveInfo.Name, this));
            }

            return drives;
        }

        public List<ImageFile> GetAllImageFiles(string directory)
        {
            if (directory != null)
            {
                if (System.IO.Directory.Exists(directory))
                {
                    var files = System.IO.Directory.GetFiles(directory, "*");
                    var listOfFiles = files.ToList<string>();
                    var listOfImageFiles = new List<ImageFile>();

                    foreach (var t in listOfFiles.ToList())
                    {
                        if (!ImageFile.IsFileAnImage(t))
                        {
                            listOfFiles.Remove(t);
                        }
                        else
                        {
                            listOfImageFiles.Add(new ImageFile(t));
                        }
                    }

                    return listOfImageFiles;
                }

                return null;
            }

            return null;
        }

        public void MoveFile(string filePath, string destinationPath, bool overwrite)
        {
            File.Move(filePath, destinationPath, overwrite);        
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }
    }
}
