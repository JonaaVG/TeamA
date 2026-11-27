
namespace GentleHealersApi.DTOs.TreatmentCode
{
    public class GetTreatmentCodeDto
    {
        public required int TreatmentCodeId { get; set; }
        public required string CareCode { get; set; }
        public required string Description { get; set; }
        public required int SpecialtyId { get; set; }
        public required int DurationQuarters { get; set; }
    }
}