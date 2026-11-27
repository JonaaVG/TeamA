namespace GentleHealersApi.DTOs.Patient
{
    public class PatientSearchResultDto
    {
        public required int PatientId { get; set;}
        public required string FirstName { get; set;}
        public required string LastName { get; set;}
        public required string Bsn { get; set; }
        public required DateOnly DateOfBirth { get; set; }
    }
}