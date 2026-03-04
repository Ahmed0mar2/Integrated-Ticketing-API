using GP.Application.Common;
using GP.Application.DTOs.Auth;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CountriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all countries for dropdown
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CountryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _context.Countries
                .OrderBy(c => c.CountryName)
                .Select(c => new CountryDto
                {
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    NationalityName = c.NationalityName,
                    PhoneCode = c.PhoneCode,
                    AllowsTrainBooking = c.AllowsTrainBooking
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CountryDto>>.SuccessResponse(countries));
        }
    }
}
