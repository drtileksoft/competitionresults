using CompetitionResults.Components.Account;
using CompetitionResults.Constants;
using CompetitionResults.Data;
using CompetitionResults.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CompetitionResults
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(AppDefaults.DefaultOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider<ApplicationUser>>();

            builder.Services.AddDbContext<CompetitionDbContext>(options =>
				options.UseSqlite(builder.Configuration.GetConnectionString("CompetitionDatabase")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<CompetitionDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            builder.Services.AddScoped<UserIdStateService>();
            builder.Services.AddScoped<CompetitionStateService>();
			builder.Services.AddScoped<CompetitionService>();
			builder.Services.AddScoped<ThrowerService>();
			builder.Services.AddScoped<CategoryService>();
			builder.Services.AddScoped<DisciplineService>();
            builder.Services.AddScoped<ResultService>();
            builder.Services.AddScoped<TranslationService>();

            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("cs"),
                new CultureInfo("sk"),
                new CultureInfo("ru"),
                new CultureInfo("fr"),
                new CultureInfo("it")
            };

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;

                options.RequestCultureProviders = new IRequestCultureProvider[]
                {
                    new CookieRequestCultureProvider
                    {
                        CookieName = CookieRequestCultureProvider.DefaultCookieName
                    },
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
            });

			builder.Services.AddScoped<NotificationHub>();

            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = AppDefaults.MaxSignalRMessageSize;
            });

            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                      new[] { "application/octet-stream" });
            });

            var app = builder.Build();

            // Initialize roles
            await InitializeRoles(app.Services.CreateScope().ServiceProvider);
			// Seed Admin user
			await SeedAdminUser(app.Services.CreateScope().ServiceProvider);

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            app.UseCors();

            app.UseAntiforgery();

            app.MapStaticAssets();

            app.UseResponseCompression();

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub");
            app.MapRazorComponents<CompetitionResults.Components.App>()
                .AddInteractiveServerRenderMode();

			app.Run();
		}

        private static async Task InitializeRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { RoleNames.Admin, RoleNames.Manager, RoleNames.User };
			IdentityResult roleResult;

			foreach (var roleName in roleNames)
			{
				var roleExist = await roleManager.RoleExistsAsync(roleName);
				if (!roleExist)
				{
					// Create the roles and seed them to the database
					roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
				}
			}
		}

        private static async Task SeedAdminUser(IServiceProvider serviceProvider)
        {
            await CreateAndAssignRole(serviceProvider, "admin@competition", RoleNames.Admin, AppDefaults.DefaultPassword);
            await CreateAndAssignRole(serviceProvider, AppDefaults.AdminEmail, RoleNames.Admin, AppDefaults.DefaultPassword);
            await CreateAndAssignRole(serviceProvider, AppDefaults.ManagerEmail, RoleNames.Manager, AppDefaults.DefaultPassword);
            await CreateAndAssignRole(serviceProvider, AppDefaults.UserEmail, RoleNames.User, AppDefaults.DefaultPassword);
		}

        private static async Task CreateAndAssignRole(IServiceProvider serviceProvider, string email, string role, string password)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DomainName = AppDefaults.Domain
                };

                var result = await userManager.CreateAsync(newUser, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, role);
                }
            }
        }

    }
}
