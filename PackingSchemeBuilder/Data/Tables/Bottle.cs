using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PackingSchemeBuilder.Data.Tables
{
    [Table("Bottle")]
    public class Bottle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Code { get; set; } 
        public int BoxId { get; set; } 

        [ForeignKey("BoxId")]
        public Box Box { get; set; }
    }
}
