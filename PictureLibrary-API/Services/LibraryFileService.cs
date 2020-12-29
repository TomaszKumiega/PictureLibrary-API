﻿using Microsoft.Extensions.Logging;
using PictureLibraryModel.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PictureLibraryModel.Services
{
    public class LibraryFileService : ILibraryFileService
    {
        private readonly ILogger<LibraryFileService> _logger;
        private readonly IFileSystemService _fileSystemService;
        public LibraryFileService(IFileSystemService fileSystemService, ILogger<LibraryFileService> logger)
        {
            _logger = logger;
            _fileSystemService = fileSystemService;
        }

        public async Task<Library> LoadLibraryAsync(FileStream fileStream)
        {
            var albumsList = new List<Tag>();
            string libraryName = null;

            if (fileStream.Length==0) throw new ArgumentException("Given stream is empty");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;

            using (var reader = XmlReader.Create(fileStream, settings))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "album")
                        {
                            var albumElement = XNode.ReadFrom(reader) as XElement;

                            var albumName = albumElement.Attribute("name").Value;
                            var imageList = new List<ImageFile>();

                            reader.ReadToDescendant("image");

                            do
                            {
                                var imageElement = XNode.ReadFrom(reader) as XElement;

                                try
                                {
                                    var image = new ImageFile(imageElement.Attribute("path").Value);
                                    imageList.Add(image);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogDebug(e, e.Message);
                                }

                            } while (reader.ReadToNextSibling("image"));

                            albumsList.Add(new Tag(albumName, imageList));
                        }

                        if (reader.Name == "library")
                        {
                            var libraryElement = XNode.ReadFrom(reader) as XElement;

                            libraryName = libraryElement.Attribute("name").Value;
                        }
                    }
                }
            }

            return new Library(fileStream.Name, libraryName, albumsList);
        }

        public Library CreateLibrary(string libraryName, FileStream fileStream)
        {
            if (fileStream==null) throw new ArgumentNullException();

            var libraryElement = new XElement("library", new XAttribute("name", libraryName));

            try
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    var xmlWriter = new XmlTextWriter(streamWriter);

                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.Indentation = 4;

                    libraryElement.Save(xmlWriter);
                }
            }
            catch (IOException e)
            {
                _logger.LogDebug(e, e.Message);
                throw new Exception("Library already exists");
            }

            return new Library(fileStream.Name, libraryName);
        }


        public async Task<ObservableCollection<Library>> GetAllLibrariesAsync()
        {
            var files = new List<string>();
            
            foreach (var t in System.IO.DriveInfo.GetDrives())
            {
                files.AddRange(await Task.Run(() => FindLibrariesInDirectory(t.RootDirectory.ToString())));
            }

            var libraries = new ObservableCollection<Library>();

            foreach (var t in files)
            {
                var fileStream = new FileStream(t, FileMode.Open);
                await Task.Run(()=> libraries.Add(LoadLibraryAsync(fileStream).Result));
            }

            return libraries;
        }

        private IEnumerable<string> FindLibrariesInDirectory(string root)
        {
            Queue<string> pending = new Queue<string>();
            pending.Enqueue(root);

            while (pending.Count != 0)
            {
                var path = pending.Dequeue();
                
                List<string> items = null;

                try
                {
                    items = System.IO.Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith("*.plib"))
                        .ToList();
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.LogDebug(e, "Unauthorized access exception while looking for libraries");
                }
                catch (Exception e)
                {
                    _logger.LogError(e,e.Message);
                }

                if(items !=null && items.Count !=0)
                    foreach (var file in items)
                        yield return file;
                try
                {
                    items = System.IO.Directory.GetDirectories(path).ToList();
                    foreach (var t in items) pending.Enqueue(t);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public async Task SaveLibrariesAsync(List<Library> libraries)
        {
            if(libraries==null) throw new ArgumentNullException();

            XElement libraryElement;
            foreach (var t in libraries)
            {
                libraryElement = new XElement("library", new XAttribute("name", t.Name));

                foreach (var a in t.Albums)
                {
                    var albumElement = new XElement("album", new XAttribute("name", a));

                    foreach (var i in a.Images)
                    {
                        var imageElement = new XElement("image", new XAttribute("path", i.FullPath));

                        albumElement.Add(imageElement);
                    }

                    libraryElement.Add(albumElement);
                }

                try
                {
                    using (var stream = new FileStream(t.FullPath, FileMode.Create))
                    {
                        var streamWriter = new StreamWriter(stream);
                        var xmlWriter = new XmlTextWriter(streamWriter);

                        xmlWriter.Formatting = Formatting.Indented;
                        xmlWriter.Indentation = 4;

                        await Task.Run(()=>libraryElement.Save(xmlWriter));
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        public async Task<Library> FindLibrary(string name)
        {
            var libraries = await GetAllLibrariesAsync();

            return libraries.FirstOrDefault(l => l.Name == name);
        }

        public async Task UpdateLibrary(Library library)
        {
            throw new NotImplementedException();
        }

        public async Task AddLibrary(Library library)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteLibrary(Library library)
        {
            throw new NotImplementedException();
        }
    }
}
