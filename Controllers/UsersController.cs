﻿using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Security.Claims;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.ViewModels;

namespace InventoryManagement.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ILogger<UsersController> logger, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Route("Search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is searching for users with query: {Query}.", currentUserId, query);
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Search request failed: query is empty.");
            return View("SearchResults", new UserSearchResultsViewModel
            {
                Query = string.Empty,
                FoundUsers = new List<UserSearchDTO>()
            });
        }
        var users = await _userService.SearchUsersAsync(query);
        var viewModel = new UserSearchResultsViewModel
        {
            Query = query,
            FoundUsers = users
        };
        return View("SearchResults", viewModel);
    }
}