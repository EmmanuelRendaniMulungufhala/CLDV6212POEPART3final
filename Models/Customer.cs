using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models
{
    public class Customer
    {
        [Key]
        [StringLength(50)]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string PartitionKey { get; set; } = "CUSTOMER";

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}