using System.ComponentModel.DataAnnotations;
namespace GentleHealersApi.DTOs.Patient
{
    public class CreatePatientDto
    {
        [Required]
        public int UserId { get; set; }
        [Required, RegularExpression(@"^\d{9}$", ErrorMessage = "Het BSN moet uit 9 cijfers bestaan.")]
        public string Bsn { get; set; } = "";

        [Required, StringLength(30)]
        public string Phone { get; set; } = "";

        [Required]
        public DateOnly DateOfBirth { get; set; }
    }
}