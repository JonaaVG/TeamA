namespace GentleHealersApi.DTOs
{
    public enum EmailType
    {
        Activation,
        PasswordReset,
        Referral
    }

    public record EmailRequest(
        EmailType Type,
        string ToEmail,
        string FirstName,
        string Link,
        string RoleName,
        IReadOnlyList<string>? ReferralCodes = null);

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string FromName { get; set; } = "De Zachte Heelmeesters";
        public string FromAddress { get; set; } = string.Empty;
    }
}