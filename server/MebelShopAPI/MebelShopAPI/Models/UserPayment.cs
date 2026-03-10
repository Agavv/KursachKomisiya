using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MebelShopAPI.Models
{
    public class UserPayment
    {
        [Key]
        public int UserId { get; set; }

        [Column("CardNumber")]
        [MaxLength(255)]
        public string CardNumber { get; set; }

        [Column("Expiry")]
        [MaxLength(255)]
        public string Expiry { get; set; }

        [Column("CVV")]
        [MaxLength(255)]
        public string CVV { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
