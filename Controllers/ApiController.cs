using Microsoft.AspNetCore.Mvc;
using CompetitionResults.Data;

namespace CompetitionResults.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : Controller
    {
        private ThrowerService _throwerService;

        public ApiController(ThrowerService throwerService)
        {
            _throwerService = throwerService;
        }

        [HttpGet("registrations/{competitionId}")]
        public async Task<IActionResult> Index(int competitionId)
        {
            var throwers = await _throwerService.GetAllThrowersAsync(competitionId);

            // Return the throwers as JSON but only name,surname and email
            var throwersDto = throwers.Select(t => new { t.Name, t.Surname, t.ClubName, t.Nationality, t.PaymentDone }).ToList();

            return Json(throwersDto);
        }
    }
}
