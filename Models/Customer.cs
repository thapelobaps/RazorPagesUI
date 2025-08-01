using System.ComponentModel.DataAnnotations;

namespace RazorPagesUI.Models;

public class Customer
{
    public int CustomerID { get; set; }

    [Required(ErrorMessage = "Customer Name is required")]
    public string CustomerName { get; set; }

    [Required(ErrorMessage = "Customer Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string CustomerEmail { get; set; }
}