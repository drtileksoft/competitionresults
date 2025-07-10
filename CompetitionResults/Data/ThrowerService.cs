using CompetitionResults.Notifications;
using CompetitionResults.Constants;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace CompetitionResults.Data
{

    public class ThrowerService
	{
		private readonly NotificationHub _notificationHub;
		private readonly CompetitionDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly TranslationService _translationService;

        public ThrowerService(CompetitionDbContext context,
                        NotificationHub notificationHub,
            IConfiguration configuration,
            TranslationService translationService)
        {
            _context = context;
                        _notificationHub = notificationHub;
            _configuration = configuration;
            _translationService = translationService;
                }

        public async Task<List<Thrower>> GetAllThrowersAsync(int competitionId)
        {
            return await _context.Throwers
                .Include(t => t.Category)
                .Include(t => t.Competition)
                .Where(c => c.CompetitionId == competitionId)
                .ToListAsync();
        }

        public async Task<Thrower> GetThrowerByIdAsync(int id)
        {
            return await _context.Throwers.FindAsync(id);
        }

        public async Task AddThrowerAsync(Thrower thrower)
		{
            if (thrower.StartingNumber == 0)
            {
                var max = await _context.Throwers
                    .Where(t => t.CompetitionId == thrower.CompetitionId)
                    .MaxAsync(t => (int?)t.StartingNumber) ?? 0;
                thrower.StartingNumber = max + 1;
            }

            _context.Throwers.Add(thrower);
            await _context.SaveChangesAsync();

            var competition = await _context.Competitions.FindAsync(thrower.CompetitionId);
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                if (competition != null &&
                    !string.IsNullOrEmpty(competition.LocalLanguage) &&
                    thrower.Nationality.ToUpper() == competition.LocalLanguage.ToUpper())
                {
                    await SendRegistrationEmailLocal(thrower);
                }
                else
                {
                    await SendRegistrationEmail(thrower);
                }
            }

			await _notificationHub.NotifyCompetitionChanged();
		}

        public async Task ResendEmailAsync(Thrower thrower)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                var competition = await _context.Competitions.FindAsync(thrower.CompetitionId);
                if (competition != null &&
                    !string.IsNullOrEmpty(competition.LocalLanguage) &&
                    thrower.Nationality.ToUpper() == competition.LocalLanguage.ToUpper())
                {
                    await SendRegistrationEmailLocal(thrower);
                }
                else
                {
                    await SendRegistrationEmail(thrower);
                }
            }
        }

        public async Task SendUnpaidEmail(Thrower thrower)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                var competition = thrower.Competition ?? _context.Competitions.Find(thrower.CompetitionId);
                if (competition != null &&
                    !string.IsNullOrEmpty(competition.LocalLanguage) &&
                    thrower.Nationality.ToUpper() == competition.LocalLanguage.ToUpper())
                {
                    var lang = competition.LocalLanguage;
                    var email = $"{await _translationService.GetValueAsync(TranslationKeys.Hello, lang)}\n\n";
                    email += string.Format(await _translationService.GetValueAsync(TranslationKeys.UnpaidEmailIntro, lang)) + "\n";
                    email += string.Format(await _translationService.GetValueAsync(TranslationKeys.ParticipantLimit, lang), thrower.Competition.MaxCompetitorCount) + "\n\n";
                    email += string.Format(await _translationService.GetValueAsync(TranslationKeys.PaymentStats, lang), thrower.Competition.Throwers.Count(t => t.PaymentDone), thrower.Competition.Throwers.Count) + "\n\n";
                    email += await _translationService.GetValueAsync(TranslationKeys.PayAsap, lang) + "\n\n";
                    email += await _translationService.GetValueAsync(TranslationKeys.ThankYou, lang) + "\n\n";
                    email += string.Format(await _translationService.GetValueAsync(TranslationKeys.Team, lang), thrower.Competition.Name);

                    SendEmail(thrower.Email, await _translationService.GetValueAsync(TranslationKeys.ImportantPaymentForCompetition, lang), email);
                }
                else
                {
                    var email = $"Hello,\n\n";
                    email += $"This email is automatically generated because you have registered for the competition and have not yet paid.\n";
                    email += $"The limit for the number of participants has been set to {thrower.Competition.MaxCompetitorCount}. Registration is final only after payment.\n\n";
                    email += $"Currently, {thrower.Competition.Throwers.Count(t => t.PaymentDone)} out of {thrower.Competition.Throwers.Count} participants have paid.\n\n";
                    email += $"Please pay as soon as possible, otherwise someone else will be faster than you and you will not be able to participate in the competition.\n\n";
                    email += $"Thank you.\n\n";
                    email += $"Team {thrower.Competition.Name}";

                    SendEmail(thrower.Email, "Important - Payment for competition", email);
                }
            }
        }

        public void SendGeneralEmail(Thrower thrower, string localMessage, string englishMessage)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                string email;
                string subject;

                var competition = thrower.Competition ?? _context.Competitions.Find(thrower.CompetitionId);
                if (!string.IsNullOrEmpty(thrower.Nationality) &&
                    competition != null &&
                    !string.IsNullOrEmpty(competition.LocalLanguage) &&
                    thrower.Nationality.ToUpper() == competition.LocalLanguage.ToUpper())
                {
                    subject = "Obecná zpráva";
                    email = localMessage;
                }
                else
                {
                    subject = "General Announcement";
                    email = englishMessage;
                }

                SendEmail(thrower.Email, subject, email);
            }
        }


        private async Task SendRegistrationEmail(Thrower thrower)
		{
			var competition = await _context.Competitions.FindAsync(thrower.CompetitionId);

			// Send email to the thrower with all details of his registration
			var email = $"You have been successfully registered to competition: {competition.Name}.\n";
			// Add all details of thrower
			email += $"Name: {thrower.Name}\n";
			email += $"Surname: {thrower.Surname}\n";
			email += $"Nickname: {thrower.Nickname}\n";
			email += $"Nationality: {thrower.Nationality}\n";
			email += $"Club name: {thrower.ClubName}\n";
			email += $"Email: {thrower.Email}\n";
			email += $"Note: {thrower.Note}\n";
			email += $"Category: {thrower.Category.Name}\n";
			email += $"Is Camping On Site: {thrower.IsCampingOnSite}\n";
			email += $"Want T-Shirt: {thrower.WantTShirt}\n";
            if (thrower.WantTShirt)
            {
                email += $"T-Shirt Size: {thrower.TShirtSize}\n";
            }

			email += $"\n";

			email += competition.EmailTemplateFooter;

            SendEmail(thrower.Email, "Registration for competition", email);
		}

                private async Task SendRegistrationEmailLocal(Thrower thrower)
                {
                        var competition = await _context.Competitions.FindAsync(thrower.CompetitionId);
                        var lang = competition.LocalLanguage;

                        // Send email to the thrower with all details of his registration
                        var email = string.Format(await _translationService.GetValueAsync(TranslationKeys.RegisteredToCompetition, lang), competition.Name) + "\n";
                        // Add all details of thrower
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Name, lang)}: {thrower.Name}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Surname, lang)}: {thrower.Surname}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Nickname, lang)}: {thrower.Nickname}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Nationality, lang)}: {thrower.Nationality}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.ClubName, lang)}: {thrower.ClubName}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Email, lang)}: {thrower.Email}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Note, lang)}: {thrower.Note}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.Category, lang)}: {thrower.Category.Name}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.CampingOnSite, lang)}: {thrower.IsCampingOnSite}\n";
                        email += $"{await _translationService.GetValueAsync(TranslationKeys.WantTShirt, lang)}: {thrower.WantTShirt}\n";
                        if (thrower.WantTShirt)
                        {
                                email += $"{await _translationService.GetValueAsync(TranslationKeys.TShirtSize, lang)}: {thrower.TShirtSize}\n";
                        }

			email += $"\n";

			email += competition.EmailTemplateFooterLocal;

                        SendEmail(thrower.Email, await _translationService.GetValueAsync(TranslationKeys.RegistrationForCompetition, lang), email);
                }

		public void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Retrieve the SmtpSettings section
                var smtpSettings = _configuration.GetSection("SmtpSettings");

                // Check if the SmtpSettings section exists
                if (!smtpSettings.Exists())
                {
                    Console.WriteLine("SMTP settings are missing in the configuration.");
                }
                else
                {
                    // Retrieve the Host setting
                    var host = smtpSettings["Host"];

                    // Check if the Host setting is present and not empty
                    if (string.IsNullOrWhiteSpace(host))
                    {
                        Console.WriteLine("SMTP Host is missing or empty in the configuration.");
                    }
                    else
                    {
                        using (var smtpClient = new SmtpClient(host))
                        {
                            smtpClient.Port = int.Parse(smtpSettings["Port"] ?? "25");

                            if (bool.TryParse(smtpSettings["EnableSsl"], out bool enableSsl) && enableSsl)
                            {
                                smtpClient.EnableSsl = true;
                            }

                            var username = smtpSettings["Username"];
                            var password = smtpSettings["Password"];

                            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                            {
                                var credentials = new NetworkCredential(username, password);
                                smtpClient.Credentials = credentials;
                            }

                            var fromEmail = smtpSettings["FromEmail"] ?? username;
                            if (string.IsNullOrEmpty(fromEmail))
                            {
                                Console.WriteLine("FromEmail is missing or empty in the configuration.");
                            }
                            else
                            {
                                var mailMessage = new MailMessage();
                                mailMessage.From = new MailAddress(fromEmail);
                                mailMessage.To.Add(toEmail);
                                mailMessage.Subject = subject;
                                mailMessage.Body = body;
                                mailMessage.IsBodyHtml = false; // Set to true if the body contains HTML

                                smtpClient.Send(mailMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }


        public async Task UpdateThrowerAsync(Thrower thrower)
        {
            _context.Throwers.Update(thrower);
            await _context.SaveChangesAsync();

			await _notificationHub.NotifyCompetitionChanged();
		}

        public async Task DeleteThrowerAsync(int id)
        {
            var thrower = await _context.Throwers.FindAsync(id);
            if (thrower != null)
            {
                _context.Throwers.Remove(thrower);
                await _context.SaveChangesAsync();

				await _notificationHub.NotifyCompetitionChanged();
			}
        }
    }

}
