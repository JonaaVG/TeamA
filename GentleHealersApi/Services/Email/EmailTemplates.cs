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
                EmailType.Referral => Referral(r),
                _ => throw new ArgumentOutOfRangeException(nameof(r), $"Onbekend e-mailtype: {r.Type}")
            };
        }
        /// <summary>
        /// Generates the subject, HTML body, and plain text body for an account activation email based on the provided EmailRequest and whether the recipient is a patient.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="isPatient">Indicates whether the recipient is a patient.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
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
        /// <summary>
        /// Generates the subject, HTML body, and plain text body for a password reset email based on the provided EmailRequest.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) PasswordReset(EmailRequest r)
        {
            return Compose(r,
                "Wachtwoord wijzigen",
                "Wachtwoord wijzigen",
                "Er is een verzoek gedaan om uw wachtwoord te wijzigen. Klik op de knop hieronder om een nieuw wachtwoord in te stellen.",
                "Wachtwoord wijzigen",
                "Heeft u dit niet aangevraagd? Dan kunt u deze e-mail negeren, uw wachtwoord blijft ongewijzigd.");
        }
        /// <summary>
        /// Generates the subject, HTML body, and plain text body for a referral email based on the provided EmailRequest.
        /// The email content varies depending on whether there is a single referral code or multiple referral codes.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) Referral(EmailRequest r)
        {
            var codes = r.ReferralCodes ?? Array.Empty<string>();
            var single = codes.Count == 1;

            var codesHtml =
                "<ul style=\"font-size:16px;font-weight:bold;letter-spacing:1px;padding-left:20px\">"
                + string.Join("", codes.Select(c => $"<li>{WebUtility.HtmlEncode(c)}</li>"))
                + "</ul>";
            var codesText = string.Join("\n", codes.Select(c => $"- {c}"));

            var intro = single
                ? "Uw huisarts heeft een doorverwijzing voor een afspraak open gezet. Gebruik onderstaande doorverwijscode om in het patiëntenportaal een afspraak te maken."
                : "Uw huisarts heeft doorverwijzingen voor afspraken open gezet. Gebruik onderstaande doorverwijscodes om in het patiëntenportaal afspraken te maken.";

            return Compose(r,
                "Uw huisarts heeft een doorverwijzing voor u klaargezet",
                "Doorverwijzing beschikbaar",
                intro,
                "Naar het patiëntenportaal",
                "Verwachtte u deze e-mail niet? Neem dan contact op met uw huisarts.",
                codesHtml,
                codesText);
        }

        /// <summary>
        /// Composes the subject, HTML body, and plain text body of an email based on the provided parameters.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="title">The title of the email.</param>
        /// <param name="intro">The introduction text of the email.</param>
        /// <param name="buttonText">The text for the call-to-action button.</param>
        /// <param name="footer">The footer text of the email.</param>
        /// <param name="extraHtml">The HTML content to include in the email.</param>
        /// <param name="extraText">The plain text content to include in the email.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) Compose(
            EmailRequest r, string subject, string title, string intro, string buttonText, string footer,
            string? extraHtml = null, string? extraText = null)
        {
            var name = WebUtility.HtmlEncode(r.FirstName);
            var link = WebUtility.HtmlEncode(r.Link);

            var html = $@"
                <div style=""font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#0f172a"">
                <h1 style=""font-size:22px;margin:0 0 16px"">{WebUtility.HtmlEncode(title)}</h1>
                <p>Beste {name},</p>
                <p>{WebUtility.HtmlEncode(intro)}</p>
                {extraHtml}
                <p style=""margin:28px 0"">
                    <a href=""{link}"" style=""background:#172554;color:#ffffff;padding:12px 24px;border-radius:12px;text-decoration:none;font-weight:bold"">
                    {WebUtility.HtmlEncode(buttonText)}
                    </a>
                </p>
                <p style=""font-size:13px;color:#64748b"">Werkt de knop niet? Kopieer dan deze link in uw browser:<br>{link}</p>
                <hr style=""border:none;border-top:1px solid #e2e8f0;margin:24px 0"">
                <p style=""font-size:13px;color:#64748b"">{WebUtility.HtmlEncode(footer)}</p>
                </div>";

            var extra = string.IsNullOrEmpty(extraText) ? "" : $"{extraText}\n\n";
            var text = $"Beste {r.FirstName},\n\n{intro}\n\n{extra}{r.Link}\n\n{footer}";

            return (subject, html, text);
        }
    }
}