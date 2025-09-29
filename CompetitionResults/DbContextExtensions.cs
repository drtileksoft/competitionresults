using AutoMapper;
using CompetitionResults.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

public static class DbContextExtensions
{

    // --- DTO package for a single competition backup ---
    public class CompetitionPackageDto
    {
        public CompetitionDto Competition { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
        public List<DisciplineDto> Disciplines { get; set; } = new();
        public List<ThrowerDto> Throwers { get; set; } = new();
        public List<ResultsDto> Results { get; set; } = new();
    }

    // --- Export: one competition + related data (IDs in JSON are original; import will rekey) ---
    public static async Task<string> ExportCompetitionToJsonAsync(
        CompetitionDbContext context,
        int competitionId,
        string? filePath = null)
    {
        var mapper = CreateMapper();

        var comp
            = await context.Competitions.AsNoTracking().FirstAsync(c => c.Id == competitionId);

        var categories
            = await context.Categories.AsNoTracking()
                .Where(x => x.CompetitionId == competitionId)
                .OrderBy(x => x.Id)
                .ToListAsync();

        var disciplines
            = await context.Disciplines.AsNoTracking()
                .Where(x => x.CompetitionId == competitionId)
                .OrderBy(x => x.Id)
                .ToListAsync();

        var throwers
            = await context.Throwers.AsNoTracking()
                .Where(x => x.CompetitionId == competitionId)
                .OrderBy(x => x.Id)
                .ToListAsync();

        var results
            = await context.Results.AsNoTracking()
                .Where(x => x.CompetitionId == competitionId)
                .OrderBy(x => x.DisciplineId).ThenBy(x => x.ThrowerId)
                .ToListAsync();

        var dto = new CompetitionPackageDto
        {
            Competition = mapper.Map<CompetitionDto>(comp),
            Categories = mapper.Map<List<CategoryDto>>(categories),
            Disciplines = mapper.Map<List<DisciplineDto>>(disciplines),
            Throwers = mapper.Map<List<ThrowerDto>>(throwers),
            Results = mapper.Map<List<ResultsDto>>(results)
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            await File.WriteAllTextAsync(filePath, json);
        }

        return json;
    }

    // ------------------------------
    // Import: if a competition with the same Name already exists, delete it (cascade) and re-import as NEW.
    //  - Snapshot its managers (ManagerId list) before delete and reattach them to the NEW competition.
    //  - Insert NEW competition from package with all children:
    //      * all NEW ids,
    //      * references remapped (Thrower.CategoryId required; Results FKs),
    //  - Users & UserRoles untouched.
    // ------------------------------
    public static async Task<int> ImportCompetitionAsNewAsync(
        CompetitionDbContext context,
        string jsonOrFilePath)
    {
        // Load JSON (path or raw json)
        string json = File.Exists(jsonOrFilePath)
            ? await File.ReadAllTextAsync(jsonOrFilePath)
            : jsonOrFilePath;

        var package = JsonSerializer.Deserialize<CompetitionPackageDto>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new InvalidOperationException("Invalid or empty competition package.");

        if (package.Competition == null)
            throw new InvalidOperationException("Package is missing Competition.");

        if (string.IsNullOrWhiteSpace(package.Competition.Name))
            throw new InvalidOperationException("Package.Competition.Name is required to match/replace existing competition.");

        // 0) If a competition with the same Name exists, snapshot managers and delete that competition (and dependents)
        var preservedManagerIds = new List<string>();

        // Exact match by Name (trim for safety); adjust if you prefer case-insensitive compare at DB level
        string targetName = package.Competition.Name.Trim();

        var existing = await context.Competitions
            .FirstOrDefaultAsync(c => c.Name == targetName);

        if (existing != null)
        {
            preservedManagerIds = await context.CompetitionManagers
                .Where(cm => cm.CompetitionId == existing.Id)
                .Select(cm => cm.ManagerId)
                .ToListAsync();

            await DeleteCompetitionCascadeAsync(context, existing.Id);
        }

        // 1) Create new Competition (NEW Id), copying all scalar props except Id
        var newComp = new Competition();
        CopyScalarProps(package.Competition, newComp, excludeProps: new[] { "Id" });
        // ensure defaults in case DTO omitted some optional fields etc.

        context.Competitions.Add(newComp);
        await context.SaveChangesAsync(); // generate new Id
        int newCompetitionId = newComp.Id;

        // 2) Reinsert children with NEW ids and remap references
        // Build old->new maps
        var oldCatToNew = new Dictionary<int, int>();
        var oldDisToNew = new Dictionary<int, int>();
        var oldThrToNew = new Dictionary<int, int>();

        // 2a) Categories (if present)
        if (package.Categories?.Count > 0)
        {
            foreach (var cDto in package.Categories)
            {
                var cat = new Category
                {
                    CompetitionId = newCompetitionId
                };
                CopyScalarProps(cDto, cat, excludeProps: new[] { "Id", "CompetitionId" });

                context.Categories.Add(cat);
                await context.SaveChangesAsync();

                oldCatToNew[cDto.Id] = cat.Id;
            }
        }

        // 2b) Disciplines — CategoryId se NEREMAPUJE (v modelu Disciplines kategorie nejsou)
        if (package.Disciplines?.Count > 0)
        {
            foreach (var dDto in package.Disciplines)
            {
                var dis = new Discipline
                {
                    CompetitionId = newCompetitionId
                };

                // Nekopíruj "Id" a "CompetitionId"; "CategoryId" z DTO ignorujeme
                CopyScalarProps(dDto, dis, excludeProps: new[] { "Id", "CompetitionId", "CategoryId" });

                context.Disciplines.Add(dis);
                await context.SaveChangesAsync();

                oldDisToNew[dDto.Id] = dis.Id;
            }
        }

        // 2c) Throwers — POVINNÉ remapování CategoryId na nově vložené kategorie
        if (package.Throwers?.Count > 0)
        {
            foreach (var tDto in package.Throwers)
            {
                var thr = new Thrower
                {
                    CompetitionId = newCompetitionId
                };

                // Zkopíruj vše kromě Id, CompetitionId a CategoryId (ten přemapujeme níže)
                CopyScalarProps(tDto, thr, excludeProps: new[] { "Id", "CompetitionId", "CategoryId" });

                // Vytažení původního CategoryId z DTO (musí existovat a mapovat se)
                var catProp = tDto.GetType().GetProperty("CategoryId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (catProp == null || !catProp.CanRead)
                    throw new InvalidOperationException("Thrower DTO is missing readable CategoryId.");

                var oldCatObj = catProp.GetValue(tDto);
                if (oldCatObj == null)
                    throw new InvalidOperationException("Thrower DTO has null CategoryId, but model requires a non-null CategoryId.");

                int oldCatId = Convert.ToInt32(oldCatObj);

                if (!oldCatToNew.TryGetValue(oldCatId, out var newCatId))
                    throw new InvalidOperationException($"Missing Category mapping for Thrower.CategoryId old Id {oldCatId}. Make sure the package includes Categories and they are imported first.");

                // Nastav remapované CategoryId na entitu
                var thrCatProp = typeof(Thrower).GetProperty("CategoryId");
                thrCatProp!.SetValue(thr, newCatId);

                context.Throwers.Add(thr);
                await context.SaveChangesAsync();

                oldThrToNew[tDto.Id] = thr.Id;
            }
        }

        // 2d) Results (composite key; create new rows with remapped FK and copy all other scalar props)
        if (package.Results?.Count > 0)
        {
            var batch = new List<CompetitionResults.Data.Results>();
            foreach (var rDto in package.Results)
            {
                // Map ThrowerId / DisciplineId
                int newThrowerId = MapRequired(oldThrToNew, rDto.ThrowerId, "ThrowerId");
                int newDisciplineId = MapRequired(oldDisToNew, rDto.DisciplineId, "DisciplineId");

                var res = new CompetitionResults.Data.Results
                {
                    CompetitionId = newCompetitionId,
                    ThrowerId = newThrowerId,
                    DisciplineId = newDisciplineId
                };

                // Copy other scalar props except FK/composite parts
                CopyScalarProps(rDto, res, excludeProps: new[] { "Id", "CompetitionId", "ThrowerId", "DisciplineId" });

                batch.Add(res);
            }

            if (batch.Count > 0)
            {
                context.Results.AddRange(batch);
                await context.SaveChangesAsync();
            }
        }

        // 3) Re-attach preserved managers to NEW competition
        if (preservedManagerIds.Count > 0)
        {
            var managerLinks = preservedManagerIds.Select(mid => new CompetitionManager
            {
                CompetitionId = newCompetitionId,
                ManagerId = mid
            }).ToList();

            context.CompetitionManagers.AddRange(managerLinks);
            await context.SaveChangesAsync();
        }

        return newCompetitionId;
    }

    // --- Utility: map required old->new with clear error messages ---
    private static int MapRequired(Dictionary<int, int> map, int oldId, string propName)
    {
        if (!map.TryGetValue(oldId, out var newId))
            throw new InvalidOperationException($"Missing mapping for {propName} old Id {oldId}.");
        return newId;
    }

    // --- Utility: copy scalar (value-type/string/nullable) props by name, excluding some ---
    private static void CopyScalarProps<TSrc, TDest>(TSrc src, TDest dest, string[]? excludeProps = null)
    {
        var excludes = new HashSet<string>(excludeProps ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        var sProps = typeof(TSrc).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var dProps = typeof(TDest).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(p => p.CanWrite)
                                  .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var sp in sProps)
        {
            if (excludes.Contains(sp.Name)) continue;
            if (!dProps.TryGetValue(sp.Name, out var dp)) continue;

            if (!IsScalarLike(sp.PropertyType)) continue;

            var val = sp.GetValue(src);
            dp.SetValue(dest, val);
        }
    }

    private static bool IsScalarLike(Type t)
    {
        var nt = Nullable.GetUnderlyingType(t) ?? t;
        return nt.IsPrimitive
            || nt.IsEnum
            || nt == typeof(string)
            || nt == typeof(decimal)
            || nt == typeof(DateTime)
            || nt == typeof(DateTimeOffset)
            || nt == typeof(TimeSpan)
            || nt == typeof(Guid);
    }

    private static bool HasWritableProperty(object obj, string propName)
        => obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.CanWrite == true;

    private static void SetIntProperty(object obj, string propName, int value)
        => obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)!.SetValue(obj, value);

    // --- Cascade delete helper for one competition (Categories, Disciplines, Throwers, Results, Managers) ---
    public static async Task DeleteCompetitionCascadeAsync(CompetitionDbContext context, int competitionId)
    {
        // Results
        var results = await context.Results
            .Where(r => r.CompetitionId == competitionId)
            .ToListAsync();
        context.Results.RemoveRange(results);

        // Throwers
        var throwers = await context.Throwers
            .Where(t => t.CompetitionId == competitionId)
            .ToListAsync();
        context.Throwers.RemoveRange(throwers);

        // Disciplines
        var disciplines = await context.Disciplines
            .Where(d => d.CompetitionId == competitionId)
            .ToListAsync();
        context.Disciplines.RemoveRange(disciplines);

        // Categories
        var categories = await context.Categories
            .Where(cat => cat.CompetitionId == competitionId)
            .ToListAsync();
        context.Categories.RemoveRange(categories);

        // Managers
        var managers = await context.CompetitionManagers
            .Where(cm => cm.CompetitionId == competitionId)
            .ToListAsync();
        context.CompetitionManagers.RemoveRange(managers);

        // Competition
        var comp = await context.Competitions.FirstOrDefaultAsync(c => c.Id == competitionId);
        if (comp != null) context.Competitions.Remove(comp);

        await context.SaveChangesAsync();
    }


    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Competition, CompetitionDto>().ReverseMap();
            cfg.CreateMap<Category, CategoryDto>().ReverseMap();
            cfg.CreateMap<Discipline, DisciplineDto>().ReverseMap();
            cfg.CreateMap<Thrower, ThrowerDto>().ReverseMap();
            cfg.CreateMap<CompetitionManager, CompetitionManagerDto>().ReverseMap();
            cfg.CreateMap<CompetitionResults.Data.Results, ResultsDto>().ReverseMap();
            cfg.CreateMap<ApplicationUser, UserDto>().ReverseMap();
            cfg.CreateMap<IdentityUserRole<string>, UserRoleDto>().ReverseMap();
        });

        return config.CreateMapper();
    }

    public static async Task<string> ExportDataToJsonAsync(CompetitionDbContext context, string filePath)
    {
        var mapper = CreateMapper();

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
            var mapper = CreateMapper();

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
