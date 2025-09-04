using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Models;
using Microsoft.Extensions.Logging;
using InventoryManagement.Web.ViewModels;

namespace InventoryManagement.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<RegisterDTO> _registerValidator;
    private readonly IValidator<LoginDTO> _loginValidator;
    private readonly IValidator<ConfirmEmailDTO> _confirmEmailValidator;
    private readonly IValidator<ForgotPasswordDTO> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDTO> _resetPasswordValidator;
    private readonly UserManager<User> _userManager;

    public AuthController(IAuthenticationService authService,
        ILogger<AuthController> logger,
        IValidator<RegisterDTO> registerValidator,
        IValidator<LoginDTO> loginValidator,
        IValidator<ConfirmEmailDTO> confirmEmailValidator,
        IValidator<ForgotPasswordDTO> forgotPasswordValidator,
        IValidator<ResetPasswordDTO> resetPasswordValidator,
        UserManager<User> userManager)
    {
        _authService = authService;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _confirmEmailValidator = confirmEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        _logger.LogInformation("User is viewing the registration page.");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDTO model)
    {
        _logger.LogInformation("Attempting to register new user with email: {Email}", model.Email);
        var validationResult = await _registerValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Registration validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.RegisterAsync(model);
        if (result.Succeeded)
        {
            _logger.LogInformation("User with email {Email} registered successfully.", model.Email);
            return RedirectToAction("ConfirmEmail", new { email = model.Email });
        }
        _logger.LogWarning("Registration failed for email: {Email}. Errors: {@Errors}", model.Email, result.Errors);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet("/Auth/AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Inventory");
        }
        _logger.LogInformation("User is viewing the login page.");
        return View();
    }
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDTO model)
    {
        _logger.LogInformation("Attempting to log in user with email: {Email}", model.Email);
        var validationResult = await _loginValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Login validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.LoginAsync(model);
        if (result.Succeeded)
        {
            _logger.LogInformation("User with email {Email} logged in successfully.", model.Email);
            return RedirectToAction("Index", "Home");
        }
        if (result.IsNotAllowed)
        {
            _logger.LogWarning("Login failed for unconfirmed email: {Email}", model.Email);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.EmailConfirmed)
            {
                await _authService.SendEmailConfirmationOtpAsync(model.Email);
                return RedirectToAction("ConfirmEmail", new { email = model.Email });
            }
        }
        _logger.LogWarning("Invalid login attempt for email: {Email}", model.Email);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User is attempting to log out.");
        await _authService.LogoutAsync();
        _logger.LogInformation("User logged out successfully.");
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string email)
    {
        _logger.LogInformation("User is requesting email confirmation for email: {Email}", email);
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation request failed: User with email {Email} not found.", email);
            return NotFound("User not found.");
        }
        var model = new ConfirmEmailViewModel { Email = email, UserId = user.Id.ToString() };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailDTO model)
    {
        _logger.LogInformation("Attempting to confirm email for user ID: {UserId}", model.UserId);
        var validationResult = await _confirmEmailValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            var viewModel = new ConfirmEmailViewModel { Email = user?.Email, UserId = model.UserId.ToString() };
            return View(viewModel);
        }
        var result = await _authService.ConfirmEmailAsync(model.UserId.ToString(), model.Code);
        if (result)
        {
            _logger.LogInformation("Email confirmed successfully for user ID: {UserId}", model.UserId);
            return RedirectToAction("EmailConfirmed", "Auth");
        }
        _logger.LogWarning("Email confirmation failed for user ID: {UserId}: Invalid or expired code.", model.UserId);
        ModelState.AddModelError(string.Empty, "Invalid or expired confirmation code.");
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult EmailConfirmed()
    {
        _logger.LogInformation("User is viewing the email confirmed page.");
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        _logger.LogInformation("User is viewing the forgot password page.");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
    {
        _logger.LogInformation("Attempting to send password reset OTP to email: {Email}", model.Email);
        var validationResult = await _forgotPasswordValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Forgot password validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        await _authService.SendPasswordResetOtpAsync(model.Email);
        _logger.LogInformation("Password reset OTP sent to email: {Email}.", model.Email);
        return RedirectToAction("ResetPassword", new { email = model.Email });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email)
    {
        _logger.LogInformation("User is viewing the reset password page for email: {Email}", email);
        var model = new ResetPasswordViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
    {
        _logger.LogInformation("Attempting to reset password for email: {Email}", model.Email);
        var validationResult = await _resetPasswordValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Password reset validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.ResetPasswordAsync(model);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset succeeded for email: {Email}", model.Email);
            return RedirectToAction("Login", "Auth");
        }
        _logger.LogWarning("Password reset failed for email: {Email}. Errors: {@Errors}", model.Email, result.Errors);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
}