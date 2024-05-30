using MagicVilla_API.Data;
using MagicVilla_API.Models;
using MagicVilla_API.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_API.Controllers
{
    //[Route("api/[controller]")]
    [Route("api/VillaAPI")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        // Dit is een HttpGet endpoint
        [HttpGet]
        public IEnumerable<VillaDTO> GetVillas()
        {
            return VillaStore.villaList;
        }

        // Voor een enkele villa is een id verplicht
        [HttpGet("{id:int}")]
        public VillaDTO GetVilla(int id)
        {
            return VillaStore.villaList.FirstOrDefault(u => u.Id == id);
        }
    }
}
