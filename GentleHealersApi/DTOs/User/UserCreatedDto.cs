namespace GentleHealersApi.DTOs.User
{
    public class UserCreatedDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string ActivationLink { get; set; } = "";
    }
}