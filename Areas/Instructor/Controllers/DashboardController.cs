using System.Security.Claims;
using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class DashboardController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var instructor = await CurrentInstructorAsync();
        if (instructor is null) return NotFound();

        ViewBag.StudentCount = await db.StudentProfiles.CountAsync(item => item.InstructorProfileId == instructor.Id);
        ViewBag.TodayCount = await db.LessonBookings.CountAsync(item =>
            item.InstructorProfileId == instructor.Id &&
            item.StartsAt.Date == DateTime.Today &&
            (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed || item.Status == BookingStatus.Completed));
        ViewBag.CompletedCount = await db.LessonBookings.CountAsync(item =>
            item.InstructorProfileId == instructor.Id && item.Status == BookingStatus.Completed);
        ViewBag.PendingCount = await db.LessonBookings.CountAsync(item =>
            item.InstructorProfileId == instructor.Id && item.Status == BookingStatus.Pending);
        ViewBag.Today = await db.LessonBookings
            .Where(item => item.InstructorProfileId == instructor.Id && item.StartsAt.Date == DateTime.Today)
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.Car)
            .OrderBy(item => item.StartsAt)
            .ToListAsync();
        return View(instructor);
    }

    private Task<InstructorProfile?> CurrentInstructorAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == userId && item.IsActive);
    }
}
