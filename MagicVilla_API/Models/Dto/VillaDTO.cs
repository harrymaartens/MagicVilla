using System.ComponentModel.DataAnnotations;

namespace MagicVilla_API.Models.Dto
{
    public class VillaDTO
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Name { get; set; }

        // Bezetting
        public int Occupancy { get; set; }

        // Square feet = Vierkante meter
        public int Sqft { get; set; }
    }
}
