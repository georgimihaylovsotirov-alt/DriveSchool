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
public sealed class ScheduleController(DriveSchoolDbContext db, BookingService bookingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var schedule = await db.LessonBookings
            .Where(item => item.InstructorProfileId == instructorId)
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.Car)
            .OrderBy(item => item.StartsAt)
            .ToListAsync();
        ViewBag.Cars = await db.Cars.Where(car => car.IsAvailable).OrderBy(car => car.MakeModel).ToListAsync();
        return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlot(BookingSlotViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Проверете въведените дата и час.";
            return RedirectToAction(nameof(Index));
        }
        var instructorId = await CurrentInstructorIdAsync();
        if (instructorId is null) return NotFound();
        var result = await bookingService.CreateAvailabilityAsync(instructorId.Value, model.CarId, model.StartsAt, model.EndsAt);
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
