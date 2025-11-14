using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Order
    {
        [Key]
        [StringLength(50)]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string PartitionKey { get; set; } = "ORDER";

        [Required]
        [StringLength(50)]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public int Quantity { get; set; }

        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}