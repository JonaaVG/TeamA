using GentleHealersApi.DTOs.Patient;
using Microsoft.Data.SqlClient;
namespace GentleHealersApi.Services.Interfaces
{

    public interface IPatientService
    {
        /// <summary>
        /// Creates a new patient record in the database for the specified practitioner asynchronously.
        /// </summary>
        /// <param name="dto">The data transfer object containing patient information.</param>
        /// <param name="practitionerUserId">The unique ID of the practitioner.</param>
        /// <returns>A task representing the asynchronous operation, with the created patient data.</returns>
        Task<PatientCreatedDto> AddPatientAsync(CreatePatientDto dto, int practitionerUserId);

        Task<PatientCreatedDto> AddPatientAsync(
            CreatePatientDto dto, int practitionerUserId,
            SqlConnection connection, SqlTransaction transaction);
    }
}