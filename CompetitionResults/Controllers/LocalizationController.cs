using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CompetitionResults.Controllers
{
    [Route("culture")]
    public class LocalizationController : Controller
    {
        [HttpPost("set")]
        [HttpGet("set")]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/"
                });

            return LocalRedirect(returnUrl);
        }
    }
}
