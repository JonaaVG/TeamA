namespace GentleHealersApi.Tests.TestData.Models
{
    public enum RoleType
    {
        Patient = 1,
        GeneralPractitioner = 2,
        Specialist = 3,
        Nurse = 4,
        AdministrativeStaff = 5,
        SystemAdministrator = 6,
        HealthInsurer = 7
    }

    public static class RoleTypeExtensions
    {
        public static string ToDisplayString(this RoleType role)
        {
            return role switch
            {
                RoleType.Patient => "Patiënt",
                RoleType.GeneralPractitioner => "Huisarts",
                RoleType.Specialist => "Specialist",
                RoleType.Nurse => "Verpleegkundige",
                RoleType.AdministrativeStaff => "Administratief medewerker",
                RoleType.SystemAdministrator => "Systeembeheerder",
                RoleType.HealthInsurer => "Zorgverzekeraar",
                _ => "Unknown"
            };
        }
    }
}