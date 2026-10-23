using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace TimeZone.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DatesController : ControllerBase
    {
        [Route("utc")]
        [HttpGet()]
        public IActionResult Get()
        {
            var pacificDates = new[]
            {
                "2025-12-01 00:00:00",
                "2025-11-01 00:00:00",
                "2026-12-01 00:00:00",
                "2026-11-01 00:00:00"
            };

            var utcDates = pacificDates.Select(pacificDate =>
            {
                var localDate = DateTime.ParseExact(
                    pacificDate,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None);

                return new
                {
                    PacificTime = pacificDate,
                    UtcTime = DateUtils.ConvertPacificToUtcTime(localDate)
                };
            });

            return Ok(utcDates);
        }

        [Route("pdt")]
        [HttpGet()]
        public IActionResult GetPacificTime()
        {
            var utcDateStrings = new[]
            {
                "2025-12-01 08:00:00",
                "2025-11-01 07:00:00",
                "2026-12-01 07:00:00",
                "2026-11-01 07:00:00"
            };

            var pacificDates = utcDateStrings.Select(utcDateString =>
            {
                var utcDate = DateTime.SpecifyKind(
                    DateTime.ParseExact(
                        utcDateString,
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None),
                    DateTimeKind.Utc);

                return new
                {
                    UtcTime = utcDate,
                    PacificTime = DateUtils.ConvertUtcToPacificTime(utcDate)
                };
            });

            return Ok(pacificDates);
        }
    }
}
