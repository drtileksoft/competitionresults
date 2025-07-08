using AutoMapper;
using CompetitionResults.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

public static class DbContextExtensions
{
    public static async Task<string> ExportDataToJsonAsync(CompetitionDbContext context, string filePath)
    {
        //user roles
        //users

        var config = new MapperConfiguration(cfg => {
            cfg.CreateMap<Competition, CompetitionDto>().ReverseMap();
            cfg.CreateMap<Category, CategoryDto>().ReverseMap();
            cfg.CreateMap<Discipline, DisciplineDto>().ReverseMap();
            cfg.CreateMap<Thrower, ThrowerDto>().ReverseMap();
            cfg.CreateMap<CompetitionManager, CompetitionManagerDto>().ReverseMap();
            cfg.CreateMap<CompetitionResults.Data.Results, ResultsDto>().ReverseMap();
            cfg.CreateMap<ApplicationUser, UserDto>().ReverseMap();
            cfg.CreateMap<IdentityUserRole<string>, UserRoleDto>().ReverseMap();
        }
        );

        var mapper = config.CreateMapper();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };
        var allData = new Dictionary<string, string>();

        allData.Add("Competitions", JsonSerializer.Serialize(mapper.Map<List<CompetitionDto>>(context.Competitions.ToList()), options));
        allData.Add("Categories", JsonSerializer.Serialize(mapper.Map<List<CategoryDto>>(context.Categories.ToList()), options));
        allData.Add("Disciplines", JsonSerializer.Serialize(mapper.Map<List<DisciplineDto>>(context.Disciplines.ToList()), options));
        allData.Add("Throwers", JsonSerializer.Serialize(mapper.Map<List<ThrowerDto>>(context.Throwers.ToList()), options));
        allData.Add("CompetitionManagers", JsonSerializer.Serialize(mapper.Map<List<CompetitionManagerDto>>(context.CompetitionManagers.ToList()), options));
        allData.Add("Results", JsonSerializer.Serialize(mapper.Map<List<ResultsDto>>(context.Results.ToList()), options));

        allData.Add("Users", JsonSerializer.Serialize(mapper.Map<List<UserDto>>(context.Users.ToList()), options));
        allData.Add("UserRoles", JsonSerializer.Serialize(mapper.Map<List<UserRoleDto>>(context.UserRoles.ToList()), options));

        var json = JsonSerializer.Serialize(allData, options);

        try
        {
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }

        return json;
    }

    public static async Task ClearDBAsync(CompetitionDbContext context)
    {
        try
        {
            // Clean the tables
            context.Results.RemoveRange(context.Results);
            context.Throwers.RemoveRange(context.Throwers);
            context.Categories.RemoveRange(context.Categories);
            context.Disciplines.RemoveRange(context.Disciplines);
            context.CompetitionManagers.RemoveRange(context.CompetitionManagers);
            context.Competitions.RemoveRange(context.Competitions);
            context.UserRoles.RemoveRange(context.UserRoles);
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to database: {ex.Message}");
        }
    }

    public static async Task ImportDataFromJsonAsync(CompetitionDbContext context, string filePath)
    {
        try
        {
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<Competition, CompetitionDto>().ReverseMap();
                cfg.CreateMap<Category, CategoryDto>().ReverseMap();
                cfg.CreateMap<Discipline, DisciplineDto>().ReverseMap();
                cfg.CreateMap<Thrower, ThrowerDto>().ReverseMap();
                cfg.CreateMap<CompetitionManager, CompetitionManagerDto>().ReverseMap();
                cfg.CreateMap<CompetitionResults.Data.Results, ResultsDto>().ReverseMap();
                cfg.CreateMap<ApplicationUser, UserDto>().ReverseMap();
                cfg.CreateMap<IdentityUserRole<string>, UserRoleDto>().ReverseMap();
            }
            );

            var mapper = config.CreateMapper();

            // Read the JSON file
            var json = await System.IO.File.ReadAllTextAsync(filePath);

            // Deserialize the JSON into a dictionary
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            var allData = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

            // Clean the tables
            context.Results.RemoveRange(context.Results);
            context.Throwers.RemoveRange(context.Throwers);
            context.Categories.RemoveRange(context.Categories);
            context.Disciplines.RemoveRange(context.Disciplines);
            context.CompetitionManagers.RemoveRange(context.CompetitionManagers);
            context.Competitions.RemoveRange(context.Competitions);
            context.UserRoles.RemoveRange(context.UserRoles);
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();

            // Deserialize and add data to context


            if (allData.ContainsKey("Users"))
            {
                var users = JsonSerializer.Deserialize<List<UserDto>>(allData["Users"], options);
                context.Users.AddRange(mapper.Map<List<ApplicationUser>>(users));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("UserRoles"))
            {
                var userRoles = JsonSerializer.Deserialize<List<UserRoleDto>>(allData["UserRoles"], options);
                context.UserRoles.AddRange(mapper.Map<List<IdentityUserRole<string>>>(userRoles));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("Competitions"))
            {
                var competitions = JsonSerializer.Deserialize<List<CompetitionDto>>(allData["Competitions"], options);
                context.Competitions.AddRange(mapper.Map<List<Competition>>(competitions));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("Categories"))
            {
                var categories = JsonSerializer.Deserialize<List<CategoryDto>>(allData["Categories"], options);
                context.Categories.AddRange(mapper.Map<List<Category>>(categories));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("Disciplines"))
            {
                var disciplines = JsonSerializer.Deserialize<List<DisciplineDto>>(allData["Disciplines"], options);
                context.Disciplines.AddRange(mapper.Map<List<Discipline>>(disciplines));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("Throwers"))
            {
                var throwers = JsonSerializer.Deserialize<List<ThrowerDto>>(allData["Throwers"], options);
                context.Throwers.AddRange(mapper.Map<List<Thrower>>(throwers));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("CompetitionManagers"))
            {
                var competitionManagers = JsonSerializer.Deserialize<List<CompetitionManagerDto>>(allData["CompetitionManagers"], options);
                context.CompetitionManagers.AddRange(mapper.Map<List<CompetitionManager>>(competitionManagers));
                await context.SaveChangesAsync();
            }

            if (allData.ContainsKey("Results"))
            {
                var results = JsonSerializer.Deserialize<List<CompetitionResults.Data.ResultsDto>>(allData["Results"], options);
                context.Results.AddRange(mapper.Map<List<CompetitionResults.Data.Results>>(results));
                await context.SaveChangesAsync();
            }

            // Save changes to the database
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading from file or saving to database: {ex.Message}");
        }
    }

    public static async Task<string> ExportSimpleDataToJsonAsync(DbContext context, string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var allData = new Dictionary<string, List<Dictionary<string, object>>>();

        foreach (var prop in context.GetType().GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
        {
            var entityType = prop.PropertyType.GetGenericArguments().First();
            var entities = await GetSimpleDataAsync(context, entityType);
            allData.Add(prop.Name, entities);
        }

        var json = JsonSerializer.Serialize(allData, options);

        try
        {
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }

        return json;
    }

    private static async Task<List<Dictionary<string, object>>> GetSimpleDataAsync(DbContext context, Type entityType)
    {
        var method = typeof(DbContextExtensions).GetMethod(nameof(GetSimpleDataGenericAsync), BindingFlags.NonPublic | BindingFlags.Static)
                                                 ?.MakeGenericMethod(entityType);

        if (method == null) throw new InvalidOperationException("The generic method 'GetSimpleDataGenericAsync' was not found.");

        var task = (Task<List<Dictionary<string, object>>>)method.Invoke(null, new object[] { context });
        return await task;
    }

    private static async Task<List<Dictionary<string, object>>> GetSimpleDataGenericAsync<TEntity>(DbContext context) where TEntity : class
    {
        var dbSet = context.Set<TEntity>();
        var data = await dbSet.ToListAsync();

        var simpleData = data.Select(entity =>
        {
            var properties = typeof(TEntity).GetProperties().Where(p => IsSimpleType(p.PropertyType));
            var dict = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                dict[property.Name] = property.GetValue(entity);
            }
            return dict;
        }).ToList();

        return simpleData;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid);
    }

}
