using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MagicVilla_API.Models
{
    public class VillaNumber
    {
        // De gebruiker kan de villa een (willekeurig) nummer geven
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VillaNo { get; set; }

        [ForeignKey("Villa")]
        public int VillaID { get; set; }

        // Dit doen we alleen in VillaNumberDTO, omdat we alleen hier de details van de Villa krijgen.
        // Als je VillaNumberDTO open kom je ook deze navigatie property tegen. Dus als hier gegevens ingevuld worden
        // wordt die automatisch verwerkt in de desbetreffende Villa.
        public Villa Villa { get; set; }

        public string SpecialDetails { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
