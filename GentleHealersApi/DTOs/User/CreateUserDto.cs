using System.ComponentModel.DataAnnotations;
namespace GentleHealersApi.DTOs.User
{
    public class CreateUserDto
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(100)]
        public string LastName { get; set; } = "";

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = "";

        [Required]
        public string RoleName { get; set; }= "Patient";
    }
}