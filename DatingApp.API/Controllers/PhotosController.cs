using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this._cloudinaryConfig = cloudinaryConfig;
            this._mapper = mapper;
            this._repo = repo;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photo = await _repo.GetPhoto(id);
            var photosForReturnDTO = _mapper.Map<PhotosForReturnDTO>(photo);
            return Ok(photosForReturnDTO);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm] PhotoForCreationDTO photoForCreationDTO)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId);
            var file = photoForCreationDTO.File;
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams(){
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            
            photoForCreationDTO.URL = uploadResult.Uri.ToString();
            photoForCreationDTO.PublicId = uploadResult.PublicId;
            
            var photo = _mapper.Map<Photo>(photoForCreationDTO);
            if (!user.Photos.Any(x => x.IsMain))
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if (await _repo.SaveAll())
            {
                var photoForReturnDTO = _mapper.Map<PhotosForReturnDTO>(photo);
                return CreatedAtRoute("GetPhoto", new { id = photo.Id }, photoForReturnDTO);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(x => x.Id == id))
                return Unauthorized();
            

            var photo = await _repo.GetPhoto(id);
            if (photo.IsMain)
                return BadRequest("This is already the main photo");

            var mainPhoto = await _repo.GetMainPhotoForUser(userId);
            mainPhoto.IsMain = false;

            photo.IsMain = true;

            if(await _repo.SaveAll())
                return NoContent();

            return BadRequest("Error occured");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(x => x.Id == id))
                return Unauthorized();
            

            var photo = await _repo.GetPhoto(id);
            if (photo.IsMain)
                return BadRequest("Cannot delete the main photo");

            if (photo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok") {
                    _repo.Delete(photo);
                }
            }
            else
                _repo.Delete(photo);

            if (await _repo.SaveAll())
                    return Ok();
            
            return BadRequest("Failed to delete photo");
        }
    }
}
