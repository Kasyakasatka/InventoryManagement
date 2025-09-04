using FluentValidation;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Services.Abstractions;
using InventoryManagement.Web.Services.Implementations;
using InventoryManagement.Web.Data.Models;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Configurations;
using FluentValidation.AspNetCore;
using InventoryManagement.Web.Models.Configurations;
using InventoryManagement.Web.Data.Models.Configurations;

namespace InventoryManagement.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)


    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddAuthentication()
       .AddGoogle(options =>
       {
            options.ClientId = configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
       })
       .AddDiscord(options =>
       {
           options.ClientId = configuration["Authentication:Discord:ClientId"]!;
           options.ClientSecret = configuration["Authentication:Discord:ClientSecret"]!;
       });
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            configuration.GetSection("IdentityOptions:Password").Bind(options.Password);
            options.Lockout.AllowedForNewUsers = false;
            options.Lockout.DefaultLockoutTimeSpan = Constants.IdentityConstants.DefaultLockoutTimeSpan;
            options.Lockout.MaxFailedAccessAttempts = Constants.IdentityConstants.DefaultMaxFailedAccessAttempts;
        })
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
        services.AddAutoMapper(typeof(Program));
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
 
        services.AddFluentValidationClientsideAdapters();
        //services.AddFluentValidation(options =>
        //{
        //    options.DisableDataAnnotationsValidation = true;
        //    options.ValidatorFactory = new ServiceProviderValidatorFactory(services);
        //    options.ImplicitlyValidateChildProperties = true;
        //});

        services.Configure<AppConfiguration>(configuration.GetSection("AppConfiguration"));
        services.Configure<IdentityConfig>(configuration.GetSection("IdentityOptions"));
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IItemLikeCommentService, ItemLikeCommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomIdGeneratorService, CustomIdGeneratorService>();
        services.AddScoped<IInventoryStatsService, InventoryStatsService>();
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}