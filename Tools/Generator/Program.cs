using System.Text;
using System.IO;
using CompetitionResults.Tests;
using CompetitionResults.Data;

var fixture = new InMemoryDbFixture();
var resultService = fixture.ResultService;
var sb = new StringBuilder();

sb.AppendLine("namespace CompetitionResults.Tests;");
sb.AppendLine("using CompetitionResults.Data;");
sb.AppendLine();
sb.AppendLine("public static class GeneratedExpectedResults");
sb.AppendLine("{");
sb.AppendLine("    public static readonly Dictionary<int, List<ResultDto>> ByDiscipline = new()");
sb.AppendLine("    {");

foreach (var discipline in SeedData.Disciplines)
{
    var results = await resultService.GetResultsByDisciplineAsync(discipline.Id, 1);
    sb.AppendLine($"        [{discipline.Id}] = new List<ResultDto>");
    sb.AppendLine("        {");
    foreach (var r in results)
    {
        string nameEscaped = r.ThrowerName.Replace("\"", "\\\"");
        string points = r.Points.HasValue ? r.Points.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "null";
        string bulls = r.BullseyeCount.HasValue ? r.BullseyeCount.Value.ToString() : "null";
        sb.AppendLine($"            new ResultDto {{ ThrowerId = {r.ThrowerId}, DisciplineId = {r.DisciplineId}, ThrowerName = \"{nameEscaped}\", CategoryId = {r.CategoryId}, Points = {points}, BullseyeCount = {bulls}, PointsAward = {r.PointsAward}, Position = {r.Position}, IsTieForMedal = {r.IsTieForMedal.ToString().ToLower()} }},");
    }
    sb.AppendLine("        },");
}

sb.AppendLine("    };\n");

var totals = await resultService.GetResultsTotalAsync(1);
sb.AppendLine("    public static readonly List<ResultDto> Overall = new List<ResultDto>");
sb.AppendLine("    {");
foreach (var r in totals)
{
    string nameEscaped = r.ThrowerName.Replace("\"", "\\\"");
    string points = r.Points.HasValue ? r.Points.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "null";
    sb.AppendLine($"        new ResultDto {{ ThrowerId = {r.ThrowerId}, ThrowerName = \"{nameEscaped}\", Points = {points} }},");
}
sb.AppendLine("    };\n}");

File.WriteAllText("GeneratedResults.txt", sb.ToString());
