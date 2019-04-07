using System;
using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.DTOs
{
    public class UserForRegisterDTO
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "Password should be at least 4 chars and max 8 chars")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "KnownAs is required")]
        public string KnownAs { get; set; }

        [Required(ErrorMessage = "DateOfBirth is required")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; }
        
        public DateTime Created { get; set; }

        public DateTime LastActive { get; set; }

        public UserForRegisterDTO()
        {
            this.Created = this.LastActive = DateTime.Now;
        }
    }
}