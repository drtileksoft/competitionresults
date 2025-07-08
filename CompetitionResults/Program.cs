using BlazorAppUsers.Areas.Identity;
using CompetitionResults.Data;
using CompetitionResults.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

namespace CompetitionResults
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("https://www.bladethrowers.cz")
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });

            // Add services to the container.
            builder.Services.AddRazorPages();
			builder.Services.AddServerSideBlazor();

			// Add AthleticsDbContext to the service collection
			builder.Services.AddDbContext<CompetitionDbContext>(options =>
				options.UseSqlite(builder.Configuration.GetConnectionString("CompetitionDatabase")));

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<CompetitionDbContext>();

            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

            builder.Services.AddScoped<UserIdStateService>();
            builder.Services.AddScoped<CompetitionStateService>();
			builder.Services.AddScoped<CompetitionService>();
			builder.Services.AddScoped<ThrowerService>();
			builder.Services.AddScoped<CategoryService>();
			builder.Services.AddScoped<DisciplineService>();
			builder.Services.AddScoped<ResultService>();

			builder.Services.AddScoped<NotificationHub>();

			builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 1024000; // 1 MB limit
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

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			//app.UseHttpsRedirection();

			app.UseCors();

			app.UseStaticFiles();

			app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

			app.UseResponseCompression();

			app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub");
			app.MapBlazorHub();
			app.MapFallbackToPage("/_Host");

			app.Run();
		}

		private static async Task InitializeRoles(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			string[] roleNames = { "Admin", "Manager", "User", "Viewer" };
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
            await CreateAndAssignRole(serviceProvider, "admin@competition", "Admin", "Xxxxxxxxxxx_1");
            await CreateAndAssignRole(serviceProvider, "admin@bladethrowers.cz", "Admin", "Xxxxxxxxxxx_1");
			await CreateAndAssignRole(serviceProvider, "manager@bladethrowers.cz", "Manager", "Xxxxxxxxxxx_1");
			await CreateAndAssignRole(serviceProvider, "user@bladethrowers.cz", "User", "Xxxxxxxxxxx_1");
			await CreateAndAssignRole(serviceProvider, "viewer@bladethrowers.cz", "Viewer", "Xxxxxxxxxxx_1");
			await CreateAndAssignRole(serviceProvider, "guest@bladethrowers.cz", "Viewer", "Guest_1");

			// Assign all users to both competitions
			//await AssignUsersToCompetitions(serviceProvider);
		}

   //     private static async Task AssignUsersToCompetitions(IServiceProvider serviceProvider)
   //     {
   //         var dbContext = serviceProvider.GetRequiredService<CompetitionDbContext>();
   //         var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

   //         // Specific user emails
   //         //string[] userEmails = { "admin@bladethrowers.cz", "manager@bladethrowers.cz", "user@bladethrowers.cz", "viewer@bladethrowers.cz", "guest@bladethrowers.cz" };
   //         //int[] competitionIds = { 1, 2 }; // The competition IDs to assign users to

			//string[] userEmails = { "admin@competition" };
			//int[] competitionIds = { 1 }; // The competition IDs to assign users to

			//foreach (var email in userEmails)
   //         {
   //             var user = await userManager.FindByEmailAsync(email);
   //             if (user != null)
   //             {
   //                 foreach (var compId in competitionIds)
   //                 {
   //                     var alreadyAssigned = dbContext.CompetitionManagers.Any(cm => cm.ManagerId == user.Id && cm.CompetitionId == compId);
   //                     if (!alreadyAssigned)
   //                     {
   //                         dbContext.CompetitionManagers.Add(new CompetitionManager { ManagerId = user.Id, CompetitionId = compId });
   //                     }
   //                 }
   //             }
   //         }

   //         await dbContext.SaveChangesAsync();
   //     }

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
                    DomainName = "bladethrowers.cz"
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
