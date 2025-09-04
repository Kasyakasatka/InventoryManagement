using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Web.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminService> _logger;
    private readonly ApplicationDbContext _context;

    public AdminService(UserManager<User> userManager, IMapper mapper, ILogger<AdminService> logger, ApplicationDbContext context)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }
    public async Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync(int count)
    {
        _logger.LogInformation("Retrieving top {Count} users for admin panel.", count);
        var users = await _context.Users
            .Take(count)
            .Select(u => new
            {
                User = u,
                UserRoles = _context.UserRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.RoleId).ToList()
            })
            .ToListAsync();
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();
        var userDTOs = users.Select(u => new UserManagementDTO
        {
            Id = u.User.Id,
            Username = u.User.UserName!,
            Email = u.User.Email!,
            IsAdmin = u.UserRoles.Contains(adminRoleId),
            IsLockedOut = u.User.LockoutEnd.HasValue && u.User.LockoutEnd.Value > DateTime.UtcNow
        }).ToList();
        _logger.LogInformation("Successfully retrieved {Count} users for admin panel.", userDTOs.Count);
        return userDTOs;
    }

    public async Task<bool> AddUserToAdminRoleAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to add user {UserId} to Admin role.", userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Failed to add user to admin role: User {UserId} not found.", userId);
            return false;
        }
        await _userManager.AddToRoleAsync(user, "Admin");
        _logger.LogInformation("User {UserId} successfully added to Admin role.", userId);
        return true;
    }

    public async Task<bool> RemoveUserFromAdminRoleAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to remove user {UserId} from Admin role.", userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Failed to remove user from admin role: User {UserId} not found.", userId);
            return false;
        }
        await _userManager.RemoveFromRoleAsync(user, "Admin");
        _logger.LogInformation("User {UserId} successfully removed from Admin role.", userId);
        return true;
    }

    public async Task<bool> BlockUserAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to block user {UserId}.", userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found. Block operation failed.", userId);
            return false;
        }
        var result = await _userManager.SetLockoutEnabledAsync(user, true);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} blocked successfully.", userId);
        }
        else
        {
            _logger.LogError("Failed to block user {UserId}. Errors: {@Errors}", userId, result.Errors);
        }
        return result.Succeeded;
    }
    public async Task<bool> UnblockUserAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to unblock user {UserId}.", userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;
        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} unblocked successfully.", userId);
        }
        else
        {
            _logger.LogError("Failed to unblock user {UserId}. Errors: {@Errors}", userId, result.Errors);
        }
        return result.Succeeded;
    }
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        _logger.LogInformation("Attempting to delete user {UserId}.", userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found. Deletion failed.", userId);
            return false;
        }
        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} deleted successfully.", userId);
        }
        else
        {
            _logger.LogError("Failed to delete user {UserId}. Errors: {@Errors}", userId, result.Errors);
        }
        return result.Succeeded;
    }
}