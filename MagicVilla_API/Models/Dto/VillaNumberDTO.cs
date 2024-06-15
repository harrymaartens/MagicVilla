using System.ComponentModel.DataAnnotations;

namespace MagicVilla_API.Models.Dto
{
    public class VillaNumberDTO
    {
        [Required]
        public int VillaNo { get; set; }

        [Required]
        public int VillaID { get; set; }

        public string SpecialDetails { get; set; }

        // Dit doen we alleen in VillaNumberDTO, omdat we alleen hier de details van de Villa krijgen.
        // Als je VillaNumber open kom je ook deze navigatie property tegen.
        public VillaDTO Villa { get; set; }
    }
}
