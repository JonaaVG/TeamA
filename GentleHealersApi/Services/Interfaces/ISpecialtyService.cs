using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Interfaces
{
    public interface ISpecialtyService
    {
        Task<IEnumerable<SpecialtyDto>> GetSpecialtiesAsync(int userId);
    }
}