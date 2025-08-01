using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;

namespace RazorPagesUI.Utils;

public static class SecurityUtils
{
    public static string HashPassword(string password, out string salt)
    {
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        //ECryptographicException
        salt = Convert.ToBase64String(saltBytes);

        using (var sha256 = SHA256.Create())
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + storedSalt);
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);
            string computedHash = Convert.ToBase64String(hashBytes);
            return computedHash == storedHash;
        }
    }

    public static bool IsValidPassword(string password)
    {
        if (password.Length < 12)
            return false;

        var hasLowerCase = new Regex(@"[a-z]");
        var hasUpperCase = new Regex(@"[A-Z]");
        var hasNumber = new Regex(@"[0-9]");
        var hasSpecialChar = new Regex(@"[!@#$%^&*(),.?""':;{}|<>]");

        return hasLowerCase.IsMatch(password) &&
               hasUpperCase.IsMatch(password) &&
               hasNumber.IsMatch(password) &&
               hasSpecialChar.IsMatch(password);
    }

    public static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
        var random = new Random();
        var password = new char[12];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        return new string(password);
    }

    public static void SendEmail(string toEmail, string subject, string body)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var smtpSettings = configuration.GetSection("SmtpSettings");
        var fromAddress = new MailAddress(smtpSettings["Username"], "RazorPagesUI");
        var toAddress = new MailAddress(toEmail);

        var smtp = new SmtpClient
        {
            Host = smtpSettings["Host"],
            Port = int.Parse(smtpSettings["Port"]),
            EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, smtpSettings["Password"])
        };

        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body
        })
        {
            smtp.Send(message);
        }
    }
}