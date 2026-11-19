using System.Net;
using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Email
{
    public static class EmailTemplates
    {
        public static (string Subject, string Html, string Text) Build(EmailRequest r)
        {
            var isPatient = r.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase);

            return r.Type switch
            {
                EmailType.Activation => Activation(r, isPatient),
                EmailType.PasswordReset => PasswordReset(r),
                _ => throw new ArgumentOutOfRangeException(nameof(r), $"Onbekend e-mailtype: {r.Type}")
            };
        }

        private static (string, string, string) Activation(EmailRequest r, bool isPatient)
        {
            var subject = isPatient
                ? "Uitnodiging voor het patiëntenportaal"
                : "Activeer uw account";

            var intro = isPatient
                ? "Uw huisarts heeft een account voor u aangemaakt in het patiëntenportaal. Klik op de knop hieronder om uw account te activeren en een wachtwoord in te stellen."
                : "Er is een account voor u aangemaakt. Klik op de knop hieronder om uw account te activeren en een wachtwoord in te stellen.";

            return Compose(r, subject, "Account activeren", intro, "Account activeren",
                "Heeft u deze uitnodiging niet verwacht? Dan kunt u deze e-mail negeren.");
        }

        private static (string, string, string) PasswordReset(EmailRequest r)
        {
            return Compose(r,
                "Wachtwoord wijzigen",
                "Wachtwoord wijzigen",
                "Er is een verzoek gedaan om uw wachtwoord te wijzigen. Klik op de knop hieronder om een nieuw wachtwoord in te stellen.",
                "Wachtwoord wijzigen",
                "Heeft u dit niet aangevraagd? Dan kunt u deze e-mail negeren, uw wachtwoord blijft ongewijzigd.");
        }

        // Gedeelde layout voor alle mails
        private static (string, string, string) Compose(
            EmailRequest r, string subject, string title, string intro, string buttonText, string footer)
        {
            var name = WebUtility.HtmlEncode(r.FirstName);
            var link = WebUtility.HtmlEncode(r.Link);

            var html = $@"
<div style=""font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#0f172a"">
  <h1 style=""font-size:22px;margin:0 0 16px"">{WebUtility.HtmlEncode(title)}</h1>
  <p>Beste {name},</p>
  <p>{WebUtility.HtmlEncode(intro)}</p>
  <p style=""margin:28px 0"">
    <a href=""{link}"" style=""background:#172554;color:#ffffff;padding:12px 24px;border-radius:12px;text-decoration:none;font-weight:bold"">
      {WebUtility.HtmlEncode(buttonText)}
    </a>
  </p>
  <p style=""font-size:13px;color:#64748b"">Werkt de knop niet? Kopieer dan deze link in uw browser:<br>{link}</p>
  <hr style=""border:none;border-top:1px solid #e2e8f0;margin:24px 0"">
  <p style=""font-size:13px;color:#64748b"">{WebUtility.HtmlEncode(footer)}</p>
</div>";

            var text = $"Beste {r.FirstName},\n\n{intro}\n\n{r.Link}\n\n{footer}";

            return (subject, html, text);
        }
    }
}