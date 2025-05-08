using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.ModelsDto;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public LocationsController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
        {
            return await _context.Locations
                .Select(l => new LocationDto
                {
                    IdLocation = l.IdLocation,
                    Name = l.Name,
                    Address = l.Addres
                })
                .ToListAsync();
        }
    }
}