using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly LibraryDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(LibraryDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("assign-admin")]
    [Authorize(Roles = "IsSuperAdmin")]
    public async Task<IActionResult> AssignAdmin(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        user.IsAdmin = true;
        await _context.SaveChangesAsync();

        return Ok("Foydalanuvchi muvaffaqiyatli Admin qilindi.");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        user.IsAdmin = false;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Foydalanuvchi muvaffaqiyatli ro'yxatdan o'tdi" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(User user)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == user.Username && u.Password == user.Password);

        if (existingUser == null)
            return Unauthorized();  // Agar foydalanuvchi topilmasa, Unauthorized qaytaring

        // Tokenni yaratish
        var token = GenerateJwtToken(existingUser);

        // Tokenni yuboring, admin bo'lsa role="Admin" bo'ladi
        return Ok(new { token, isAdmin = existingUser.IsAdmin });
    }


    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }
        if (user.IsSuperAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "IsSuperAdmin"));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}