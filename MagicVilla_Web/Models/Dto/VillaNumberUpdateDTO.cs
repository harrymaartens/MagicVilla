using System.ComponentModel.DataAnnotations;

namespace MagMagicVilla_WebicVilla_API.Models.Dto
{
    public class VillaNumberUpdateDTO
    {
        [Required]
        public int VillaNo { get; set; }

        [Required]
        public int VillaID { get; set; }

        public string SpecialDetails { get; set; }
    }
}
