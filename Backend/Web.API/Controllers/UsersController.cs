using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.API.Domain.Auth;
using Web.API.Persistence.Context;
using Web.API.Persistence.Services.Shared;

namespace Web.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ApiControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultLimit = 10;
    private const int MaxLimit = 100;

    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Users(
        [FromQuery] int page = DefaultPage,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        page = Math.Max(DefaultPage, page);
        limit = Math.Clamp(limit, 1, MaxLimit);

        var query = _db.Users.AsNoTracking()
            .Include(x => x.Role)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.Username.Contains(term) ||
                x.FullName.Contains(term) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.Phone != null && x.Phone.Contains(term)));
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.FullName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            statusCode = StatusCodes.Status200OK,
            message = "Data retrieved successfully",
            data = users.Select(ToResponse),
            pagination = new
            {
                page,
                limit,
                total,
                totalPage = Math.Max(1, (int)Math.Ceiling(total / (double)limit))
            }
        });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles()
    {
        var roles = await _db.Roles.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return ApiOk(roles);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        return user is null
            ? ApiNotFound("User was not found.")
            : ApiOk(ToResponse(user));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var username = NormalizeRequired(request.Username, "Username is required.");
            var fullName = NormalizeRequired(request.FullName, "Full name is required.");
            var password = NormalizeRequired(request.Password, "Password is required.");
            var email = NormalizeOptional(request.Email);
            var phone = NormalizeOptional(request.Phone);
            var role = await GetActiveRoleAsync(request.RolesId);

            await EnsureUsernameAvailableAsync(username);
            await EnsureEmailAvailableAsync(email);

            var salt = AuthPasswordHasher.CreateSalt();
            var now = DateTime.Now;
            var user = new AppUser
            {
                Username = username,
                FullName = fullName,
                Email = email,
                Phone = phone,
                RolesId = role.Id,
                Role = role,
                IsActive = request.IsActive,
                PasswordSalt = salt,
                PasswordHash = AuthPasswordHasher.HashPassword(password, salt),
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return ApiCreated(ToResponse(user), "User created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user is null)
            {
                return ApiNotFound("User was not found.");
            }

            var username = NormalizeRequired(request.Username, "Username is required.");
            var fullName = NormalizeRequired(request.FullName, "Full name is required.");
            var email = NormalizeOptional(request.Email);
            var phone = NormalizeOptional(request.Phone);
            var role = await GetActiveRoleAsync(request.RolesId);

            await EnsureUsernameAvailableAsync(username, id);
            await EnsureEmailAvailableAsync(email, id);
            await EnsureLastActiveAdminRemainsAsync(user, role, request.IsActive);

            user.Username = username;
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.RolesId = role.Id;
            user.Role = role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var salt = AuthPasswordHasher.CreateSalt();
                user.PasswordSalt = salt;
                user.PasswordHash = AuthPasswordHasher.HashPassword(request.Password.Trim(), salt);
            }

            await _db.SaveChangesAsync();

            return ApiOk(ToResponse(user), "User updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user is null)
            {
                return ApiNotFound("User was not found.");
            }

            await EnsureLastActiveAdminRemainsAsync(user, user.Role, false);

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return ApiOk(ToResponse(user), "User deactivated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    private async Task<AppRole> GetActiveRoleAsync(int rolesId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Id == rolesId && x.IsActive);
        return role ?? throw new ArgumentException("Selected role was not found or is inactive.");
    }

    private async Task EnsureUsernameAvailableAsync(string username, int? currentUserId = null)
    {
        var exists = await _db.Users.AnyAsync(x =>
            x.Username == username &&
            (!currentUserId.HasValue || x.Id != currentUserId.Value));

        if (exists)
        {
            throw new ArgumentException("Username already exists.");
        }
    }

    private async Task EnsureEmailAvailableAsync(string? email, int? currentUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var exists = await _db.Users.AnyAsync(x =>
            x.Email == email &&
            (!currentUserId.HasValue || x.Id != currentUserId.Value));

        if (exists)
        {
            throw new ArgumentException("Email already exists.");
        }
    }

    private async Task EnsureLastActiveAdminRemainsAsync(AppUser user, AppRole? nextRole, bool nextIsActive)
    {
        if (!user.IsActive || !IsAdmin(user.Role))
        {
            return;
        }

        if (nextIsActive && IsAdmin(nextRole))
        {
            return;
        }

        var otherActiveAdmins = await _db.Users
            .Include(x => x.Role)
            .CountAsync(x => x.Id != user.Id && x.IsActive && x.Role != null && x.Role.Name == AppUserRole.ADMIN.ToString());

        if (otherActiveAdmins == 0)
        {
            throw new InvalidOperationException("At least one active admin user must remain.");
        }
    }

    private static UserResponse ToResponse(AppUser user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RolesId = user.RolesId,
            Role = string.IsNullOrWhiteSpace(user.Role?.Name)
                ? AppUserRole.VIEWER.ToString()
                : user.Role.Name.Trim().ToUpperInvariant(),
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

    private static bool IsAdmin(AppRole? role) =>
        string.Equals(role?.Name, AppUserRole.ADMIN.ToString(), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new ArgumentException(errorMessage);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
