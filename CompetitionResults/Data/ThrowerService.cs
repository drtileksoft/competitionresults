using CompetitionResults.Notifications;
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

        public ThrowerService(CompetitionDbContext context,
			NotificationHub notificationHub,
            IConfiguration configuration)
        {
            _context = context;
			_notificationHub = notificationHub;
            _configuration = configuration;
		}

        public async Task<List<Thrower>> GetAllThrowersAsync(int competitionId)
        {
            return await _context.Throwers.Where(c => c.CompetitionId == competitionId).ToListAsync();
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

            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                if (thrower.Nationality.ToUpper() == "CZ")
                {
                    await SendRegistrationEmailCZ(thrower);
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
				if (thrower.Nationality.ToUpper() == "CZ")
				{
					await SendRegistrationEmailCZ(thrower);
				}
				else
				{
					await SendRegistrationEmail(thrower);
				}
			}
		}

        public void SendUnpaidEmail(Thrower thrower)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                if (thrower.Nationality.ToUpper() == "CZ")
                {
                    var email = $"Dobrý den,\n\n";
                    email += $"Tento email je automaticky generován, protože jste se zaregistrovali na soutěž a ještě jste nezaplatili.\n";
                    email += $"Limit pro počet účastníků byl nastaven na {thrower.Competition.MaxCompetitorCount}.Registrace je finální až po zaplacení.\n\n";
                    email += $"Aktuálně má zaplaceno {thrower.Competition.Throwers.Count(t => t.PaymentDone)} z {thrower.Competition.Throwers.Count} účastníků.\n\n";
                    email += $"Prosím, zaplaťte co nejdříve, jinak Vás předběhne někdo jiný a nebudete se moci zúčastnit soutěže.\n\n";
                    email += $"Děkujeme.\n\n";
                    email += $"Tým {thrower.Competition.Name}";

                    SendEmail(thrower.Email, "Dulezite - Platba za registraci", email);
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

        public void SendGeneralEmail(Thrower thrower)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                string email;
                string subject;

                if (thrower.Nationality.ToUpper() == "CZ")
                {
                    subject = "Aktualizace cateringu";

                    email = "Vážení účastníci,\n\n";

                    email += "S pozdravem,\nTým UKAT World Cup";
                }
                else
                {
                    subject = "Catering Update";

                    email = "Dear participants,\n\n";

                    email += "Warm regards,\nUKAT World Cup Organizing Team";
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

		private async Task SendRegistrationEmailCZ(Thrower thrower)
		{
			var competition = await _context.Competitions.FindAsync(thrower.CompetitionId);

			// Send email to the thrower with all details of his registration
			var email = $"Byl/a jste úspěšně registrován/a na soutěž: {competition.Name}.\n";
			// Add all details of thrower
			email += $"Jméno: {thrower.Name}\n";
			email += $"Příjmení: {thrower.Surname}\n";
			email += $"Přezdívka: {thrower.Nickname}\n";
			email += $"Národnost: {thrower.Nationality}\n";
			email += $"Jméno klubu: {thrower.ClubName}\n";
			email += $"Email: {thrower.Email}\n";
			email += $"Poznámka: {thrower.Note}\n";
			email += $"Kategorie: {thrower.Category.Name}\n";
			email += $"Kempování na místě: {thrower.IsCampingOnSite}\n";
			email += $"Chci tričko: {thrower.WantTShirt}\n";
			if (thrower.WantTShirt)
			{
				email += $"Velikost trička: {thrower.TShirtSize}\n";
			}

			email += $"\n";

			email += competition.EmailTemplateFooterCZ;

			SendEmail(thrower.Email, "Registrace do závodu", email);
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
