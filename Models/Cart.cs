using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerUsername { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    }
}