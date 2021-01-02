﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PictureLibrary_API.Model;
using PictureLibrary_API.Repositories;

namespace PictureLibrary_API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private IImageRepository _imageRepository;
        private ILibraryRepository _libraryRepository;

        public ImageController(ILogger<ImageController> logger, IImageRepository imageRepository, ILibraryRepository libraryRepository)
        {
            _logger = logger;
            _imageRepository = imageRepository;
            _libraryRepository = libraryRepository;
        }

        [HttpGet]
        public async Task<ActionResult<byte[]>> GetImage([FromBody] ImageFile imageFile)
        {
            var library = await _libraryRepository.GetBySourceAsync(imageFile.LibrarySource);

            if (library == null) return BadRequest();

            if (library.Images.Find(x => x.Source == imageFile.Source) == null)
            {
                return BadRequest();
            }

            //TODO: check if library is owned by the current user

            var image = await _imageRepository.GetBySourceAsync(imageFile.Source);

            if (image == null)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpGet("{librarySource}")]
        public async Task<ActionResult<IEnumerable<byte[]>>> GetImages(string librarySource)
        {
            var library = await _libraryRepository.GetBySourceAsync(librarySource);

            if (library == null) return BadRequest();

            //TODO: check if current user has access to the library

            var images = await _imageRepository.GetAllAsync(library.Name);

            if (images == null)
            {
                return NotFound();
            }

            return Ok(images);
        }

        [HttpPut("imageFile")]
        public async Task<IActionResult> PutImage([FromBody] ImageFile imageFile)
        {
            var library = await _libraryRepository.GetBySourceAsync(imageFile.LibrarySource);

            if (library == null) return BadRequest();

            //TODO: check if current user has access to the library

            var updatedImage = await _imageRepository.UpdateAsync(imageFile);

            library.Images.Remove(library.Images.Find(x => x.Source == imageFile.Source));
            library.Images.Add(updatedImage);

            await _libraryRepository.UpdateAsync(library);

            return NoContent();
        }

        [HttpPut("imageContent")]
        public async Task<IActionResult> PutImage([FromBody] Image image)
        {
            var library = await _libraryRepository.GetBySourceAsync(image.ImageFile.LibrarySource);

            if (library == null) return BadRequest();

            //TODO: check if current user has access to the library

            var updatedImage = await _imageRepository.UpdateAsync(image);

            library.Images.Remove(library.Images.Find(x => x.Source == image.ImageFile.Source));
            library.Images.Add(updatedImage);

            await _libraryRepository.UpdateAsync(library);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<ImageFile>> PostImage([FromBody] Image image)
        {
            var library = await _libraryRepository.GetBySourceAsync(image.ImageFile.LibrarySource);

            if (library == null) return BadRequest("Library doesn't exist");

            //TODO: check if current user has access to the library

            var imageFile = await _imageRepository.AddAsync(image);

            library.Images.Add(imageFile);
            await _libraryRepository.UpdateAsync(library);

            return CreatedAtAction("GetImage", new { name = imageFile.Name }, imageFile);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteImage(string source)
        {
            try
            {
                await _imageRepository.RemoveAsync(source);
            }
            catch(Exception e)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}