using System.ComponentModel.DataAnnotations;

namespace MagicVilla_API.Models.Dto
{
    public class VillaDTO
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Name { get; set; }

        public string Details { get; set; }

        [Required]
        public double Rate { get; set; }

        // Bezetting
        public int Occupancy { get; set; }

        // Square feet = Vierkante meter
        public int Sqft { get; set; }

        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }

        // Voorzieningen
        public string Amenity { get; set; }
    }
}
