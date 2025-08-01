using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorPagesUI.Models;
using System.Data.SqlClient;

namespace RazorPagesUI.Pages;

public class ManageCustomersModel : PageModel
{
    private readonly ILogger<ManageCustomersModel> _logger;
    private readonly string _connectionString;

    public ManageCustomersModel(ILogger<ManageCustomersModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [BindProperty]
    public Customer Customer { get; set; }

    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }
    public List<Customer> Customers { get; set; }

    public void OnGet()
    {
        LoadCustomers();
    }

    public IActionResult OnPostAddCustomer()
    {
        if (!ModelState.IsValid)
        {
            LoadCustomers();
            return Page();
        }

        List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@CustomerName", Customer.CustomerName),
            new SqlParameter("@CustomerEmail", Customer.CustomerEmail),
            new SqlParameter("@Action", "ADD")
        };

        try
        {
            int rowsAffected = ExecuteNonQuery("sp_CustomerTable", parameters);
            if (rowsAffected > 0)
            {
                SuccessMessage = "Customer added successfully.";
                ModelState.Clear();
                LoadCustomers();
                return Page();
            }
            else
            {
                ErrorMessage = "Failed to add customer.";
                LoadCustomers();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding customer");
            ErrorMessage = "An error occurred while adding the customer: " + ex.Message;
            LoadCustomers();
            return Page();
        }
    }

    public IActionResult OnPostUpdateCustomer()
    {
        if (!ModelState.IsValid)
        {
            LoadCustomers();
            return Page();
        }

        List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@CustomerID", Customer.CustomerID),
            new SqlParameter("@CustomerName", Customer.CustomerName),
            new SqlParameter("@CustomerEmail", Customer.CustomerEmail),
            new SqlParameter("@Action", "UPDATE")
        };

        try
        {
            int rowsAffected = ExecuteNonQuery("sp_CustomerTable", parameters);
            if (rowsAffected > 0)
            {
                SuccessMessage = "Customer updated successfully.";
                ModelState.Clear();
                LoadCustomers();
                return Page();
            }
            else
            {
                ErrorMessage = "Failed to update customer.";
                LoadCustomers();
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            ErrorMessage = "An error occurred while updating the customer: " + ex.Message;
            LoadCustomers();
            return Page();
        }
    }

    public IActionResult OnGetDeleteCustomer(int id)
    {
        List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@CustomerID", id),
            new SqlParameter("@Action", "DELETE")
        };

        try
        {
            int rowsAffected = ExecuteNonQuery("sp_CustomerTable", parameters);
            if (rowsAffected > 0)
            {
                SuccessMessage = "Customer deleted successfully.";
                ModelState.Clear();
                LoadCustomers();
                return RedirectToPage("/ManageCustomers");
            }
            else
            {
                ErrorMessage = "Failed to delete customer.";
                LoadCustomers();
                return RedirectToPage("/ManageCustomers");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer");
            ErrorMessage = "An error occurred while deleting the customer: " + ex.Message;
            LoadCustomers();
            return RedirectToPage("/ManageCustomers");
        }
    }

    private void LoadCustomers()
    {
        Customers = GetCustomersFromView("vw_Customers");
    }

    private int ExecuteNonQuery(string procedureName, List<SqlParameter> parameters)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddRange(parameters.ToArray());
                return command.ExecuteNonQuery();
            }
        }
    }

    private List<Customer> GetCustomersFromView(string viewName, List<SqlParameter> parameters = null)
    {
        List<Customer> customers = new List<Customer>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string sqlQuery = viewName == "vw_CustomersSearch"
                ? $"SELECT CustomerID, CustomerName, CustomerEmail FROM vw_Customers WHERE CustomerName LIKE @SearchTerm OR CustomerEmail LIKE @SearchTerm"
                : $"SELECT CustomerID, CustomerName, CustomerEmail FROM {viewName}";
            using (SqlCommand command = new SqlCommand(sqlQuery, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Customer customer = new Customer
                        {
                            CustomerID = (int)reader["CustomerID"],
                            CustomerName = (string)reader["CustomerName"],
                            CustomerEmail = (string)reader["CustomerEmail"]
                        };
                        customers.Add(customer);
                    }
                }
            }
        }
        return customers;
    }
}