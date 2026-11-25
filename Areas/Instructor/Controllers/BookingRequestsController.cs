using System.Security.Claims;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class BookingRequestsController(DriveSchoolDbContext db, BookingService bookingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var requests = await db.LessonBookings
            .Where(item => item.InstructorProfileId == instructorId && item.Status == BookingStatus.Pending)
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.Car)
            .OrderBy(item => item.StartsAt)
            .ToListAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, bool approve)
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var result = await bookingService.ReviewRequestAsync(id, instructorId.Value, approve);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> CurrentInstructorIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.InstructorProfiles
            .Where(item => item.ApplicationUserId == userId && item.IsActive)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();
    }
}
