using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Account;

public class LoginModel(SignInManager<ApplicationUser> signIn, ILogger<LoginModel> log) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    [FromQuery(Name = "ReturnUrl")]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await signIn.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            log.LogInformation("User {Email} signed in.", Input.Email);
            return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
        }
        if (result.IsLockedOut)
        {
            ErrorMessage = "Konto vorübergehend gesperrt.";
            return Page();
        }

        ErrorMessage = "Ungültige Anmeldedaten.";
        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}
