using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class LessonsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.LessonBookings
            .Where(item => item.Status == BookingStatus.Completed)
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Car)
            .Include(item => item.Lesson)!.ThenInclude(item => item!.Evaluation)
            .OrderByDescending(item => item.StartsAt)
            .ToListAsync();
        return View(items);
    }
}
