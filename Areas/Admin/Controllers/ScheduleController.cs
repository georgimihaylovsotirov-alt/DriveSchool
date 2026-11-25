using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class ScheduleController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateTime? from)
    {
        var start = (from ?? DateTime.Today).Date;
        var items = await db.LessonBookings
            .Where(item => item.StartsAt >= start && item.StartsAt < start.AddDays(14))
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Car)
            .OrderBy(item => item.StartsAt)
            .ToListAsync();
        ViewBag.From = start;
        return View(items);
    }
}
