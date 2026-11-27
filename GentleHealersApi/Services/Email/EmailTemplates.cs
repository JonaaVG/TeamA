using System.Globalization;
using System.Net;
using System.Resources;
using GentleHealersApi.DTOs;

namespace GentleHealersApi.Services.Email
{
    public static class EmailTemplates
    {
        private static readonly ResourceManager Texts =
            new("GentleHealersApi.Resources.EmailTexts", typeof(EmailTemplates).Assembly);

        /// <summary>
        /// Looks up a text in the resource files for the given culture and fills in any {0}, {1}, ... placeholders.
        /// Falls back to the Dutch (neutral) resource file when a key is missing in the requested language,
        /// and returns the key itself when it does not exist at all, so typos are easy to spot.
        /// </summary>
        private static string T(CultureInfo culture, string key, params object[] args)
        {
            var text = Texts.GetString(key, culture) ?? key;
            return args.Length == 0 ? text : string.Format(culture, text, args);
        }

        /// <summary>
        /// Resolves the language code of the request to a culture. Falls back to Dutch when the code is empty or unknown.
        /// </summary>
        private static CultureInfo GetCulture(string? language)
        {
            try
            {
                return CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(language) ? "nl" : language);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo("nl");
            }
        }

        public static (string Subject, string Html, string Text) Build(EmailRequest r)
        {
            var isPatient = r.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase);
            var culture = GetCulture(r.Language);

            return r.Type switch
            {
                EmailType.Activation => Activation(r, culture, isPatient),
                EmailType.PasswordReset => PasswordReset(r, culture),
                EmailType.Referral => Referral(r, culture),
                _ => throw new ArgumentOutOfRangeException(nameof(r), $"Onbekend e-mailtype: {r.Type}")
            };
        }

        /// <summary>
        /// Generates the subject, HTML body, and plain text body for an account activation email based on the provided EmailRequest and whether the recipient is a patient.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="c">The culture (language) the email is written in.</param>
        /// <param name="isPatient">Indicates whether the recipient is a patient.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) Activation(EmailRequest r, CultureInfo c, bool isPatient)
        {
            var suffix = isPatient ? "Patient" : "Staff";

            return Compose(r, c,
                T(c, $"Activation_Subject_{suffix}"),
                T(c, "Activation_Title"),
                T(c, $"Activation_Intro_{suffix}"),
                T(c, "Activation_Title"),
                T(c, "Activation_Footer"));
        }

        /// <summary>
        /// Generates the subject, HTML body, and plain text body for a password reset email based on the provided EmailRequest.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="c">The culture (language) the email is written in.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) PasswordReset(EmailRequest r, CultureInfo c)
        {
            return Compose(r, c,
                T(c, "PasswordReset_Subject"),
                T(c, "PasswordReset_Title"),
                T(c, "PasswordReset_Intro"),
                T(c, "PasswordReset_Button"),
                T(c, "PasswordReset_Footer"));
        }

        /// <summary>
        /// Generates the subject, HTML body, and plain text body for a referral email based on the provided EmailRequest.
        /// The email shows the number of referrals and uses a singular or plural intro depending on that number.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="c">The culture (language) the email is written in.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) Referral(EmailRequest r, CultureInfo c)
        {
            var count = r.ReferralCount ?? 0;

            var intro = count == 1
                ? T(c, "Referral_Intro_Single")
                : T(c, "Referral_Intro_Multiple", count);

            var summary = T(c, "Referral_Count", count);
            var summaryHtml =
                $"<p style=\"font-size:16px;font-weight:bold\">{WebUtility.HtmlEncode(summary)}</p>";

            return Compose(r, c,
                T(c, "Referral_Subject"),
                T(c, "Referral_Title"),
                intro,
                T(c, "Referral_Button"),
                T(c, "Referral_Footer"),
                summaryHtml,
                summary);
        }

        /// <summary>
        /// Composes the subject, HTML body, and plain text body of an email based on the provided parameters.
        /// </summary>
        /// <param name="r">The email request containing the recipient information and content.</param>
        /// <param name="c">The culture (language) the email is written in.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="title">The title of the email.</param>
        /// <param name="intro">The introduction text of the email.</param>
        /// <param name="buttonText">The text for the call-to-action button.</param>
        /// <param name="footer">The footer text of the email.</param>
        /// <param name="extraHtml">The HTML content to include in the email.</param>
        /// <param name="extraText">The plain text content to include in the email.</param>
        /// <returns>A tuple containing the subject, HTML body, and plain text body of the email.</returns>
        private static (string, string, string) Compose(
            EmailRequest r, CultureInfo c, string subject, string title, string intro, string buttonText, string footer,
            string? extraHtml = null, string? extraText = null)
        {
            var greeting = T(c, "Greeting", r.FirstName);
            var buttonFallback = T(c, "ButtonFallback");
            var link = WebUtility.HtmlEncode(r.Link);

            var html = $@"
                <div style=""font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#0f172a"">
                <h1 style=""font-size:22px;margin:0 0 16px"">{WebUtility.HtmlEncode(title)}</h1>
                <p>{WebUtility.HtmlEncode(greeting)}</p>
                <p>{WebUtility.HtmlEncode(intro)}</p>
                {extraHtml}
                <p style=""margin:28px 0"">
                    <a href=""{link}"" style=""background:#172554;color:#ffffff;padding:12px 24px;border-radius:12px;text-decoration:none;font-weight:bold"">
                    {WebUtility.HtmlEncode(buttonText)}
                    </a>
                </p>
                <p style=""font-size:13px;color:#64748b"">{WebUtility.HtmlEncode(buttonFallback)}<br>{link}</p>
                <hr style=""border:none;border-top:1px solid #e2e8f0;margin:24px 0"">
                <p style=""font-size:13px;color:#64748b"">{WebUtility.HtmlEncode(footer)}</p>
                </div>";

            var extra = string.IsNullOrEmpty(extraText) ? "" : $"{extraText}\n\n";
            var text = $"{greeting}\n\n{intro}\n\n{extra}{r.Link}\n\n{footer}";

            return (subject, html, text);
        }
    }
}