using System.Text.Json.Serialization;

namespace Web.API.Domain.Auth;

public enum AppUserRole
{
    ADMIN,
    SUPERVISOR,
    OPERATOR,
    VIEWER
}

public class AppRole
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("role_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class AppUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("roles_id")]
    public int RolesId { get; set; }

    [JsonPropertyName("role")]
    public AppRole? Role { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordSalt { get; set; } = string.Empty;

    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class UserResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("roles_id")]
    public int RolesId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public UserResponse User { get; set; } = new();
}

public class CreateUserRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("roles_id")]
    public int RolesId { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("roles_id")]
    public int RolesId { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}
