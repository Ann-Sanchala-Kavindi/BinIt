using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartWaste.Application.Common.Exceptions;
using SmartWaste.Application.DTOs.Auth;
using SmartWaste.Application.Interfaces;
using SmartWaste.Domain.Common;
using SmartWaste.Domain.Entities;

namespace SmartWaste.Infrastructure.Services;

/// <summary>
/// Infrastructure service implementing authentication, citizen registration, and JWT token issuance.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterCitizenAsync(RegisterRequest request)
    {
        // 1. Check for duplicate email
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new DuplicateEmailException(request.Email);
        }

        // 2. Create the AppUser instance (always default active, timestamps in UTC)
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new IdentityOperationException(result.Errors.Select(e => e.Description));
        }

        // 3. Strictly enforce Citizen role assignment (public registration cannot choose roles)
        if (!await _roleManager.RoleExistsAsync(AppRoles.Citizen))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Citizen));
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Citizen);

        // 4. Generate JWT access token
        var roles = new List<string> { AppRoles.Citizen };
        var token = GenerateJwtToken(user, roles, out var expiresAt);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = AppRoles.Citizen
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // 1. Locate user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new InvalidCredentialsException();
        }

        // 2. Verify account is active
        if (!user.IsActive)
        {
            throw new AccountInactiveException();
        }

        // 3. Verify password
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new InvalidCredentialsException();
        }

        // 4. Fetch roles and determine primary role
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? AppRoles.Citizen;

        // 5. Generate token
        var token = GenerateJwtToken(user, roles, out var expiresAt);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = primaryRole
            }
        };
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? string.Empty;

        return new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = primaryRole,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public string GenerateJwtToken(AppUser user, IList<string> roles, out DateTime expiresAt)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var keyString = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(keyString))
        {
            // Secure fallback for local development and testing
            keyString = "SmartWasteSecureSecretKeyForDevelopmentAndTesting123456!";
        }

        var issuer = jwtSection["Issuer"] ?? "SmartWaste.Api";
        var audience = jwtSection["Audience"] ?? "SmartWaste.Clients";
        var expiryMinutes = double.TryParse(jwtSection["ExpiryMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 60.0;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
