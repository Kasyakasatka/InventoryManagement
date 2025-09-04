using InventoryManagement.Web.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IUserService
{
    Task<IEnumerable<UserSearchDTO>> SearchUsersAsync(string query);
}