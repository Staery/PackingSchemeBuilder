using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PackingSchemeBuilder.Data.Tables
{
    public class Box
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 
        public string Code { get; set; } 
        public int PalletId { get; set; }

        [ForeignKey("PalletId")]
        public Pallet Pallet { get; set; }
    }
}
