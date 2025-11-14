using ABC_Retailer.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retailer.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product is required")]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}