using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorPagesUI.Models;
using System.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace RazorPagesUI.Pages;

public class RegisterModel : PageModel
{
    private readonly ILogger<RegisterModel> _logger;
    private readonly string _connectionString;

    public RegisterModel(ILogger<RegisterModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [BindProperty]
    public User RegisterUser { get; set; }

    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Utils.SecurityUtils.IsValidPassword(RegisterUser.Password))
        {
            ErrorMessage = "Password must be at least 12 characters and contain at least one lowercase letter, one uppercase letter, one number, and one special character.";
            return Page();
        }

        string salt;
        string hashedPassword = Utils.SecurityUtils.HashPassword(RegisterUser.Password, out salt);

        List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@Fullname", RegisterUser.FullName),
            new SqlParameter("@EmailAddress", RegisterUser.Email),
            new SqlParameter("@Passwords", hashedPassword),
            new SqlParameter("@Salt", salt),
            new SqlParameter("@Action", "ADD")
        };

        try
        {
            int rowsAffected = ExecuteNonQuery("sp_UserTable", parameters);
            if (rowsAffected > 0)
            {
                SuccessMessage = "Registration successful! Please log in.";
                return Page();
            }
            else
            {
                ErrorMessage = "Registration failed. Email may already be taken.";
                return Page();
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during registration");
            if (ex.Number == 2601) // Duplicate key exception
            {
                ErrorMessage = "Email address is already taken.";
            }
            else
            {
                ErrorMessage = "An error occurred during registration. Please try again.";
            }
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            ErrorMessage = "An unexpected error occurred: " + ex.Message;
            return Page();
        }
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
}