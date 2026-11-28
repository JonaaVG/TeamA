using GentleHealersApi.DTOs.Patient;
using GentleHealersApi.DTOs.User;

namespace GentleHealersApi.Services.Interfaces.Patient
{
    public interface IPatientRegistrationService
    {
        Task<(UserCreatedDto User, PatientCreatedDto Patient)> RegisterPatientAsync(
            RegisterPatientDto dto,
            int practitionerUserId);
    }
}