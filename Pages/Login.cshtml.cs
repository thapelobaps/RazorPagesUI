using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorPagesUI.Models;
using System.Data.SqlClient;

namespace RazorPagesUI.Pages;

public class LoginModel : PageModel
{
    private readonly ILogger<LoginModel> _logger;
    private readonly string _connectionString;

    public LoginModel(ILogger<LoginModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [BindProperty]
    public User LoginUser { get; set; }

    public string ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("sp_UserTable", connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@EmailAddress", LoginUser.Email);
                command.Parameters.AddWithValue("@Action", "LOGIN");

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string storedHashedPassword = reader["Passwords"].ToString();
                        string storedSalt = reader["Salt"].ToString();
                        string fullName = reader["Fullname"].ToString();

                        bool passwordMatch = Utils.SecurityUtils.VerifyPassword(LoginUser.Password, storedHashedPassword, storedSalt);

                        if (passwordMatch)
                        {
                            Response.Cookies.Append("FullName", fullName);
                            Response.Cookies.Append("Email", LoginUser.Email);
                            return RedirectToPage("/Index");
                        }
                        else
                        {
                            ErrorMessage = "Invalid login credentials.";
                            return Page();
                        }
                    }
                    else
                    {
                        ErrorMessage = "Invalid login credentials.";
                        return Page();
                    }
                }
            }
        }
    }
}