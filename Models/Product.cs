using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Product
    {
        [Key]
        [StringLength(50)]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string PartitionKey { get; set; } = "PRODUCT";

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public int StockAvailable { get; set; } = 0;
    }
}