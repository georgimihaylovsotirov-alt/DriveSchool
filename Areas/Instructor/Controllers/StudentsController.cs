using System.Security.Claims;
using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class StudentsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var students = await db.StudentProfiles
            .Where(item => item.InstructorProfileId == instructorId && item.IsActive)
            .Include(item => item.ApplicationUser)
            .OrderBy(item => item.ApplicationUser.FullName)
            .ToListAsync();
        return View(students);
    }

    public async Task<IActionResult> Details(int id)
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var student = await db.StudentProfiles
            .Where(item => item.Id == id && item.InstructorProfileId == instructorId)
            .Include(item => item.ApplicationUser)
            .Include(item => item.Bookings.Where(booking => booking.Status == BookingStatus.Completed))
                .ThenInclude(booking => booking.Lesson)
            .Include(item => item.TestResults)
                .ThenInclude(result => result.TheoryTest)
            .FirstOrDefaultAsync();
        if (student is null) return NotFound();
        return View(student);
    }

    private async Task<int?> CurrentInstructorIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.InstructorProfiles
            .Where(item => item.ApplicationUserId == userId)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();
    }
}
