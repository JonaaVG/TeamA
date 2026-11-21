using GentleHealersApi.DTOs.User;

namespace GentleHealersApi.DTOs.Patient
{
    public class RegisterPatientDto
    {
        public CreateUserDto User { get; set; } = null!;
        public string Bsn { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
    }
}