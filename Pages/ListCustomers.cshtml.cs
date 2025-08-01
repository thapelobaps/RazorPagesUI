using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorPagesUI.Models;
using System.Data.SqlClient;

namespace RazorPagesUI.Pages;

public class ListCustomersModel : PageModel
{
    private readonly ILogger<ListCustomersModel> _logger;
    private readonly string _connectionString;

    public ListCustomersModel(ILogger<ListCustomersModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public List<Customer> Customers { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; }

    public void OnGet()
    {
        Customers = GetCustomersList();
    }

    private List<Customer> GetCustomersList()
    {
        List<SqlParameter> parameters = new List<SqlParameter>();
        string viewName = "vw_Customers";

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            viewName = "vw_CustomersSearch";
            parameters.Add(new SqlParameter("@SearchTerm", "%" + SearchTerm + "%"));
        }

        List<Customer> customers = GetCustomersFromView(viewName, parameters);

        if (!string.IsNullOrEmpty(SortOrder))
        {
            switch (SortOrder)
            {
                case "name_asc":
                    customers = customers.OrderBy(c => c.CustomerName).ToList();
                    break;
                case "name_desc":
                    customers = customers.OrderByDescending(c => c.CustomerName).ToList();
                    break;
                case "email_asc":
                    customers = customers.OrderBy(c => c.CustomerEmail).ToList();
                    break;
                case "email_desc":
                    customers = customers.OrderByDescending(c => c.CustomerEmail).ToList();
                    break;
            }
        }
        return customers;
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