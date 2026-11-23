using GentleHealersApi.DTOs.Patient;
using GentleHealersApi.DTOs.User;
using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services
{
    public class PatientRegistrationService : IPatientRegistrationService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IUserService _userService;
        private readonly IPatientService _patientService;

        public PatientRegistrationService(
            ISqlConnectionFactory connectionFactory,
            IUserService userService,
            IPatientService patientService)
        {
            _connectionFactory = connectionFactory;
            _userService = userService;
            _patientService = patientService;
        }

        public async Task<(UserCreatedDto User, PatientCreatedDto Patient)> RegisterPatientAsync(
            RegisterPatientDto dto, int practitionerUserId)
        {
            using var connection = await _connectionFactory.GetConnectionAsync(practitionerUserId);
            using var transaction = connection.BeginTransaction();
            try
            {
                dto.User.RoleName = "Patient";

                var user = await _userService.CreateUserAsync(dto.User, connection, transaction);

                var patient = await _patientService.AddPatientAsync(
                    new CreatePatientDto
                    {
                        UserId = user.UserId,
                        Bsn = dto.Bsn,
                        DateOfBirth = dto.DateOfBirth
                    },
                    practitionerUserId, connection, transaction);

                await transaction.CommitAsync();
                return (user, patient);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}