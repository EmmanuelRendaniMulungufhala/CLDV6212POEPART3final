using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABC_Retailer.Models
{
    public class User
    {
        [Key]
        [StringLength(50)]
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [StringLength(50)]
        public string PartitionKey { get; set; } = "USER";

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        // NotMapped means this property won't be stored in the database
        // It's only used temporarily during registration/login
        [NotMapped]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}