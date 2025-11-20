using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourViet.Data;
using TourViet.Models;
using TourViet.Services;

namespace TourViet.Controllers;

public class AdminController : Controller
{
    private readonly TourBookingDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AdminController(TourBookingDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<IActionResult> AddAdministrativeStaffUsers()
    {
        // Find the AdministrativeStaff role
        var adminStaffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "AdministrativeStaff");
        if (adminStaffRole == null)
        {
            return Content("AdministrativeStaff role not found in database.");
        }

        // Create sample AdministrativeStaff users
        var sampleUsers = new[]
        {
            new { Username = "adminstaff1", Email = "adminstaff1@tour.com", FullName = "John Tour Guide", Phone = "0987654321", Address = "123 Tour Street, HCMC" },
            new { Username = "adminstaff2", Email = "adminstaff2@tour.com", FullName = "Jane Tour Guide", Phone = "0987654322", Address = "456 Travel Avenue, HN" },
            new { Username = "adminstaff3", Email = "adminstaff3@tour.com", FullName = "Bob Tour Guide", Phone = "0987654323", Address = "789 Adventure Road, DN" }
        };

        var password = "Password123!";

        foreach (var userData in sampleUsers)
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userData.Email && !u.IsDeleted);
            if (existingUser != null)
            {
                continue; // Skip if user already exists
            }

            // Hash the password
            var (hash, salt) = _passwordHasher.HashPassword(password);

            // Create new user
            var user = new User
            {
                UserID = Guid.NewGuid(),
                Username = userData.Username,
                Email = userData.Email,
                FullName = userData.FullName,
                Phone = userData.Phone,
                Address = userData.Address,
                PasswordHash = hash,
                PasswordSalt = salt,
                PasswordAlgo = "SHA2_512+iter1000",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign the AdministrativeStaff role
            var userRole = new UserRole
            {
                UserID = user.UserID,
                RoleID = adminStaffRole.RoleID,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        return Content("AdministrativeStaff users added successfully!");
    }
}