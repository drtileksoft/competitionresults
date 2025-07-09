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
                    email += $"Limit pro počet účastníků byl nastaven na 130. Nyní pro obrovský zájem navyšujeme na 150. Registrace je finální až po zaplacení.\n\n";
                    email += $"Aktuálně má zaplaceno {thrower.Competition.Throwers.Count(t => t.PaymentDone)} z {thrower.Competition.Throwers.Count} účastníků.\n\n";
                    email += $"Prosím, zaplaťte co nejdříve, jinak Vás předběhne někdo jiný a nebudete se moci zúčastnit soutěže.\n\n";
                    email += $"Termín pro platbu byl sice stanoven do 30. dubna 2025, ale zájem o registraci je enormní.\n\n";
                    email += $"Pokud to tedy s registrací myslíte opravdu vážně a chcete se zúčastnit, zaplaťte do 10. ledna 2025, jinak Vaše registrace bude postoupena dalším zájemcům.\n\n";
                    email += $"Děkujeme.\n\n";
                    email += $"Tým {thrower.Competition.Name}";

                    SendEmail(thrower.Email, "Dulezite - Platba za registraci", email);
                }
                else
                {
                    var email = $"Hello,\n\n";
                    email += $"This email is automatically generated because you have registered for the competition and have not yet paid.\n";
                    email += $"The limit for the number of participants has been set to 130. Due to high demand, we are now increasing the number to 150. Registration is final only after payment.\n\n";
                    email += $"Currently, {thrower.Competition.Throwers.Count(t => t.PaymentDone)} out of {thrower.Competition.Throwers.Count} participants have paid.\n\n";
                    email += $"Please pay as soon as possible, otherwise someone else will be faster than you and you will not be able to participate in the competition.\n\n";
                    email += $"The deadline for payment was set until April 30, 2025, but the interest in registration is enormous.\n\n";
                    email += $"So if you are really serious about registration and want to participate, pay till 10. of January 2025 otherwise your registration will be forwarded to other interested parties.\n\n";
                    email += $"Thank you.\n\n";
                    email += $"Team {thrower.Competition.Name}";

                    SendEmail(thrower.Email, "Important - Payment for competition", email);
                }
            }
        }

        private decimal CalculateTargetPayment(Thrower thrower)
        {
            if (thrower.PaymentDone)
            {
                if (thrower.Payment < 200) // Assume EUR
                {
                    return thrower.WantTShirt ? 100 : 90;
                }
                else // Assume CZK
                {
                    return thrower.WantTShirt ? 2450 : 2200;
                }
            }
            return 0;
        }

        private decimal GetTolerance(double? payment)
        {
            if (payment.HasValue)
            {
                return payment.Value < 200 ? 2 : 50; // 2 EUR or 50 CZK tolerance
            }
            else
            {
                return 0;
            }
        }

        public void SendMealOrderEmail(Thrower thrower)
        {
            if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
            {
                string subject;
                string email;

                bool isCzech = thrower.Nationality.ToUpper() == "CZ";
                string currency = thrower.Payment < 200 ? "EUR" : "CZK";

                subject = isCzech ? "U.K.A.T. World Cup 2025 update" : "U.K.A.T. World Cup 2025 Update";

                // Start of email
                email = isCzech
                    ? "Vážení účastníci,\n\nsoutěž UKAT World Cup začíná už za 8 dní a nemůžeme se dočkat, až se s vámi všemi setkáme!\n\n"
                    : "Dear participants,\n\nThe UKAT World Cup competition starts in just 8 days and we can't wait to see you all!\n\n";

                // Difference handling
                decimal target = CalculateTargetPayment(thrower);
                decimal paid = thrower.Payment.HasValue ? Convert.ToDecimal(thrower.Payment.Value) : 0;
                decimal difference = target - paid;
                bool shouldHighlight = Math.Abs(difference) > GetTolerance(thrower.Payment) && difference > 0;

                if (shouldHighlight)
                {
                    string diffLine = isCzech
                                    ? $"Podle našich záznamů zbývá doplatit částka {difference:0.00} {currency}, kterou je nutné uhradit při registraci.\n\n"
                                    : $"According to our records, you still owe an amount of {difference:0.00} {currency}, which must be paid during registration.\n\n";

                    string reasons = isCzech
                        ? "Tato částka může vzniknout z několika důvodů:\n- objednali jste tričko, ale částka nebyla ještě zaplacena\n- rozdíl kvůli převodu měny (CZK/EUR)\n- rozdíl kvůli poplatkům za transakci (např. PayPal)\n\n"
                        : "This amount may be due to one or more of the following reasons:\n- you ordered a T-shirt but haven't paid for it yet\n- exchange rate difference (CZK/EUR)\n- transaction fees (e.g. PayPal)\n\n";

                    email += diffLine + reasons;
                }

                // Catering info
                string cateringIntro = isCzech
                    ? "Zajistíme stravování pro všechny zájemce.\nNa základě vašich objednávek připravíme 80 obědů a 50 večeří.\nCena je pouze 8–9 € a platí se přímo u stánku.\n\n"
                    : "We will provide food for all of you interested.\nBased on your orders we will have 80 meals for lunch and 50 meals for dinner.\nPrice is just 8–9€ and is paid right at the food truck.\n\n";

                string mealList = isCzech
                    ? """
				Jídelníček:

				Čtvrtek - oběd: Hovězí guláš, houskový knedlík – 219 Kč / 9 EUR  
				Čtvrtek - večeře: Vepřový/kuřecí řízek, šťouchaný brambor – 199 Kč / 8 EUR  
				Pátek - oběd: Vepřový steak, opečený brambor – 199 Kč / 8 EUR  
				Pátek - večeře: Kuřecí směs, bramboráčky – 199 Kč / 8 EUR  
				Sobota - oběd: Smažený sýr, brambor – 199 Kč / 8 EUR  
				Sobota - večeře: Boloňské špagety – 199 Kč / 8 EUR
				"""
                    : """
				Menu:

				Thursday - Lunch: Beef goulash, bread dumplings – 219 CZK / 9 EUR  
				Thursday - Dinner: Pork/chicken schnitzel, mashed potatoes – 199 CZK / 8 EUR  
				Friday - Lunch: Pork steak, roasted potatoes – 199 CZK / 8 EUR  
				Friday - Dinner: Chicken mix, potato pancakes – 199 CZK / 8 EUR  
				Saturday - Lunch: Fried cheese, potatoes – 199 CZK / 8 EUR  
				Saturday - Dinner: Bolognese spaghetti – 199 CZK / 8 EUR
				""";

                email += cateringIntro + mealList;

                // Closing
                email += isCzech
                    ? "\n\nTěšíme se na vás!\n\nS pozdravem,\nTým UKAT World Cup"
                    : "\n\nWe look forward to seeing you!\n\nWarm regards,\nUKAT World Cup Organizing Team";


                SendEmail(thrower.Email, subject, email);
            }
        }


        //public void SendMealOrderEmail(Thrower thrower)
        //{
        //    if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
        //    {
        //        string email;
        //        string subject;

        //        if (thrower.Nationality.ToUpper() == "CZ")
        //        {
        //            subject = "Aktualizace cateringu";

        //            email = "Vážení účastníci,\n\n";

        //            email += "S pozdravem,\nTým UKAT World Cup";
        //        }
        //        else
        //        {
        //            subject = "Catering Update";

        //            email = "Dear participants,\n\n";

        //            email += "Warm regards,\nUKAT World Cup Organizing Team";
        //        }

        //        SendEmail(thrower.Email, subject, email);
        //    }
        //}

        //public void SendMealOrderEmail(Thrower thrower)
        //{
        //    if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
        //    {
        //        string email;
        //        string subject;

        //        if (thrower.Nationality.ToUpper() == "CZ")
        //        {
        //            subject = "Aktualizace cateringu";

        //            email = "Vážení účastníci,\n\n";
        //            email += "O možnost cateringu projevilo zájem pouze 50 z celkových 150 registrovaných účastníků. Bohužel je toto číslo příliš nízké na to, aby cateringová společnost považovala naši akci za realizovatelnou. Vyslání personálu a vybavení by pro ně nebylo ekonomicky výhodné a s největší pravděpodobností dají přednost větší akci, která se koná ve stejném termínu.\n\n";
        //            email += "Z tohoto důvodu bude online objednávkový systém nyní uzavřen a všechny již uhrazené platby budou **v plné výši vráceny**.\n\n";
        //            email += "Obdržíme však seznam již zadaných objednávek a použijeme jej jako základ pro zajištění **alternativního stravování**. Uděláme maximum pro to, abychom vám mohli nabídnout jídlo v počtu odpovídajícímu předchozím objednávkám na jednotlivé dny.\n\n";
        //            email += "Upozorňujeme, že **nabídka jídel se nemusí přesně shodovat s původní**, ale zajistíme, abyste měli co jíst. Aktuálně jednáme o možnostech jako **hovězí guláš, kuřecí řízek, vepřové přírodní plátky** a další.\n\n";
        //            email += "👉 **Platba za jídlo bude probíhat na místě.**\n\n";
        //            email += "Děkujeme za pochopení.\n\n";
        //            email += "S pozdravem,\nTým UKAT World Cup";
        //        }
        //        else
        //        {
        //            subject = "Catering Update";

        //            email = "Dear participants,\n\n";
        //            email += "Only 50 out of the 150 registered attendees have shown interest in the catering option. Unfortunately, this number is too low for the catering company to consider our event a viable order. Sending staff and equipment would not be cost-effective for them, and they will likely prioritize a larger event scheduled for the same dates.\n\n";
        //            email += "As a result, the online ordering system will now be closed, and any payments already made will be **refunded in full**.\n\n";
        //            email += "However, we will receive a list of the orders that were submitted and will use this as a basis to explore **alternative meal arrangements**. We will do our best to provide food according to the number of pre-orders for each day.\n\n";
        //            email += "Please note that the **menu might not match the original offer exactly**, but we will make sure you are well-fed. We are currently looking into options such as **beef goulash, chicken schnitzel, and pork cutlets**, among others.\n\n";
        //            email += "👉 **Payment for food will be handled on site.**\n\n";
        //            email += "Thank you for your understanding.\n\n";
        //            email += "Warm regards,\nUKAT World Cup Organizing Team";
        //        }

        //        SendEmail(thrower.Email, subject, email);
        //    }
        //}

        //public void SendMealOrderEmail(Thrower thrower)
        //{
        //    if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
        //    {
        //        string email;

        //        if (thrower.Nationality.ToUpper() == "CZ")
        //        {
        //            email = "🛑 UZÁVĚRKA OBJEDNÁVEK JÍDLA JE 25. 5. 2025!\n\n";
        //            email += "Objednávejte zde: https://order.fullpartyservis.cz/\n";
        //            email += "Nezasílejte žádné platby – objednávka zatím stačí.\n\n";
        //            email += "❗ Pokud do 25. 5. nebude dostatek objednávek, catering bude zrušen, platby vráceny a jídlo vyřešíme jinak.\n";

        //            SendEmail(thrower.Email, "PŘIPOMÍNKA – Objednávka jídla do 25. 5.", email);
        //        }
        //        else
        //        {
        //            email = "🛑 MEAL ORDER DEADLINE IS MAY 25, 2025!\n\n";
        //            email += "Place your order here: https://order.fullpartyservis.cz/\n";
        //            email += "Do NOT send any payments yet – just place the order.\n\n";
        //            email += "❗ If there are not enough orders by May 25, catering will be cancelled, payments refunded, and we’ll handle food another way.\n";

        //            SendEmail(thrower.Email, "REMINDER – Meal order deadline May 25", email);
        //        }
        //    }
        //}


        //public void SendMealOrderEmail(Thrower thrower)
        //{
        //    if (thrower.Email != null && !thrower.DoNotSendRegistrationEmail)
        //    {
        //        if (thrower.Nationality.ToUpper() == "CZ")
        //        {
        //            var email = "!!! JÍDLO - DŮLEŽITÉ\n";
        //            email += "OBJEDNÁVEJTE ZDE: 👉👉👉 https://order.fullpartyservis.cz/\n\n";
        //            email += "📢 Důležité upozornění pro všechny účastníky UKAT World Cup – objednávky jídel jsou nyní dostupné! 🍽️\n\n";
        //            email += "Abychom zajistili hladký průběh akce pro všechny, nabízíme možnost předobjednat si jídlo přímo na místě soutěže za velmi rozumnou cenu.\n\n";
        //            email += "👉 Předobjednání jídla je vysoce doporučeno a velmi důležité.\n";
        //            email += "Bez dostatečného počtu objednávek nebude cateringová společnost schopna tuto službu zajistit.\n\n";
        //            email += "Předobjednáním si zajistíte:\n";
        //            email += "✅ Jídlo přímo na místě (nebudete muset během soutěže odcházet!)\n";
        //            email += "✅ Pomoc s tím, aby se catering mohl uskutečnit\n";
        //            email += "✅ Podporu hladkého a organizovaného průběhu soutěže bez zbytečných prodlev\n\n";
        //            email += "Důležité:\n";
        //            email += "My jako organizátoři z cateringu nemáme žádný zisk. Naopak — část nákladů dokonce dotujeme, abychom pokryli vývoj objednávkového systému a nabídli vám tuto možnost co nejpohodlněji.\n\n";
        //            email += "Jde čistě o to, abychom vám všem zajistili pohodlné a cenově dostupné jídlo přímo na akci.\n\n";
        //            email += "Prosíme o co nejrychlejší objednání — čím dříve, tím lépe.\n";
        //            email += "Uzávěrka objednávek se rychle blíží a vaše včasná reakce bude velmi důležitá.\n\n";
        //            email += "Děkujeme za podporu a pochopení. Těšíme se na vás na UKAT World Cup! 🏆";

        //            SendEmail(thrower.Email, "DŮLEŽITÉ - Objednávka jídla na soutěži", email);
        //        }
        //        else
        //        {
        //            var email = "!!! MEALS - IMPORTANT\n";
        //            email += "ORDER HERE: 👉👉👉 https://order.fullpartyservis.cz/\n\n";
        //            email += "📢 Important notice for all UKAT World Cup participants – Meal Orders now available! 🍽️\n\n";
        //            email += "To ensure a smooth and enjoyable event for everyone, we are offering the option to pre-order meals directly at the competition venue at a very reasonable price.\n\n";
        //            email += "👉 Ordering your meal in advance is highly recommended and very important.\n";
        //            email += "Without a sufficient number of orders, the catering company will not be able to provide this service.\n\n";
        //            email += "By pre-ordering, you will:\n";
        //            email += "✅ Secure your meal right at the venue (no need to leave during the competition!)\n";
        //            email += "✅ Help us ensure the catering can go ahead\n";
        //            email += "✅ Support a smooth and organized competition schedule without delays\n\n";
        //            email += "Important to know:\n";
        //            email += "We as organizers do not make any profit from the catering. On the contrary — we are even subsidizing part of the costs to cover the development of the ordering system to make this option available and as smooth as possible for you.\n\n";
        //            email += "This is purely about making sure you all have convenient and affordable meals right at the event.\n\n";
        //            email += "Please place your orders as soon as possible — the sooner, the better.\n";
        //            email += "The deadline for orders is approaching fast, and your early action will make a big difference.\n\n";
        //            email += "Thank you for your support and understanding. We look forward to seeing you all at the UKAT World Cup! 🏆";

        //            SendEmail(thrower.Email, "IMPORTANT - Meal order for the competition", email);
        //        }
        //    }
        //}


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
