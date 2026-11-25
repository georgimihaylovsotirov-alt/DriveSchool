using System.Security.Claims;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class LessonsController(DriveSchoolDbContext db, BookingService bookingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var lessons = await db.LessonBookings
            .Where(item => item.InstructorProfileId == instructorId &&
                (item.Status == BookingStatus.Completed || item.Status == BookingStatus.Confirmed))
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.Lesson)!.ThenInclude(item => item!.Evaluation)
            .OrderByDescending(item => item.StartsAt)
            .ToListAsync();
        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var booking = await db.LessonBookings
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id &&
                item.InstructorProfileId == instructorId &&
                item.Status == BookingStatus.Confirmed);
        if (booking is null) return NotFound();
        ViewBag.StudentName = booking.StudentProfile?.ApplicationUser.FullName;
        ViewBag.StartsAt = booking.StartsAt;
        return View(new CompleteLessonViewModel { BookingId = booking.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteLessonViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var result = await bookingService.CompleteAsync(
            model.BookingId,
            instructorId.Value,
            model.Summary,
            model.VehicleControl,
            model.RoadAwareness,
            model.Confidence,
            model.Comment);
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
