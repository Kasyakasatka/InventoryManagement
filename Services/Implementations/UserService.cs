using AutoMapper;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<User> userManager, IMapper mapper, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<IEnumerable<UserSearchDTO>> SearchUsersAsync(string query)
    {
        _logger.LogInformation("Searching for users with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<UserSearchDTO>();
        }
        var formattedQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => $"{w}:*"));
        var users = await _userManager.Users
            .Where(u => u.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        var userDTOs = users
            .Select(u => _mapper.Map<UserSearchDTO>(u))
            .ToList();
        _logger.LogInformation("Found {Count} users for query.", userDTOs.Count);
        return userDTOs;
    }
}