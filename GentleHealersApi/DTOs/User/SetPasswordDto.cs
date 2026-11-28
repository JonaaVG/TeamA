using System.ComponentModel.DataAnnotations;

namespace GentleHealersApi.DTOs.User
{
    public class SetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }

        [MinLength(12, ErrorMessage = "Wachtwoord moet minimaal 12 tekens hebben.")]
        public string Password { get; set; }
    }
}
