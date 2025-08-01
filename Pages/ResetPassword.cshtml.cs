using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace RazorPagesUI.Pages;

public class ResetPasswordModel : PageModel
{
    private readonly ILogger<ResetPasswordModel> _logger;
    private readonly string _connectionString;

    public ResetPasswordModel(ILogger<ResetPasswordModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [BindProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string Email { get; set; }

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

        string newPassword = Utils.SecurityUtils.GenerateRandomPassword();
        string salt;
        string hashedPassword = Utils.SecurityUtils.HashPassword(newPassword, out salt);

        List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@EmailAddress", Email),
            new SqlParameter("@Passwords", hashedPassword),
            new SqlParameter("@Salt", salt),
            new SqlParameter("@Action", "RESET")
        };

        try
        {
            int rowsAffected = ExecuteNonQuery("sp_UserTable", parameters);
            if (rowsAffected > 0)
            {
                try
                {
                    Utils.SecurityUtils.SendEmail(Email, "Your New Password", "Your new password is: " + newPassword);
                    SuccessMessage = "A new password has been sent to your email address.";
                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email");
                    ErrorMessage = "Failed to send the email. Please check your email settings and try again. However, the password in the database has been reset.";
                    return Page();
                }
            }
            else
            {
                ErrorMessage = "Email address not found.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            ErrorMessage = "An error occurred while resetting your password: " + ex.Message;
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