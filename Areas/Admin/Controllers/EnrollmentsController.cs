using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class EnrollmentsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Enrollments
            .Include(item => item.StudentProfile).ThenInclude(item => item.ApplicationUser)
            .OrderBy(item => item.Status)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();
        ViewBag.Instructors = await db.InstructorProfiles
            .Where(item => item.IsActive)
            .Include(item => item.ApplicationUser)
            .OrderBy(item => item.ApplicationUser.FullName)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(EnrollmentReviewViewModel model)
    {
        if (!ModelState.IsValid || (model.Status != EnrollmentStatus.Approved && model.Status != EnrollmentStatus.Rejected))
        {
            TempData["Error"] = "Изберете валидно решение за заявката.";
            return RedirectToAction(nameof(Index));
        }
        var enrollment = await db.Enrollments
            .Include(item => item.StudentProfile)
            .FirstOrDefaultAsync(item => item.Id == model.Id && item.Status == EnrollmentStatus.Pending);
        if (enrollment is null)
        {
            TempData["Error"] = "Заявката вече е обработена.";
            return RedirectToAction(nameof(Index));
        }
        enrollment.Status = model.Status;
        enrollment.ReviewedAt = DateTime.UtcNow;
        if (model.Status == EnrollmentStatus.Approved && model.InstructorProfileId.HasValue)
        {
            var instructorExists = await db.InstructorProfiles.AnyAsync(item =>
                item.Id == model.InstructorProfileId.Value && item.IsActive);
            if (!instructorExists)
            {
                TempData["Error"] = "Избраният инструктор не е активен.";
                return RedirectToAction(nameof(Index));
            }
            enrollment.StudentProfile.InstructorProfileId = model.InstructorProfileId;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = model.Status == EnrollmentStatus.Approved
            ? "Заявката е одобрена."
            : "Заявката е отказана.";
        return RedirectToAction(nameof(Index));
    }
}
