
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class StudentsController(
    DriveSchoolDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .Include(item => item.InstructorProfile)
                .ThenInclude(item => item!.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(item =>
                item.ApplicationUser.FullName.Contains(term) ||
                item.ApplicationUser.Email!.Contains(term));
        }

        ViewBag.Search = search;

        return View(await query
            .OrderBy(item => item.ApplicationUser.FullName)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadInstructorsAsync();
        return View(new StudentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadInstructorsAsync();
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim()
        };

        var created = await userManager.CreateAsync(user, model.Password);

        if (!created.Succeeded)
        {
            AddErrors(created);
            await LoadInstructorsAsync();
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Student);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(roleResult);
            await LoadInstructorsAsync();
            return View(model);
        }

        var profile = new StudentProfile
        {
            ApplicationUserId = user.Id,
            LicenseCategory = model.LicenseCategory,
            InstructorProfileId = model.InstructorProfileId
        };

        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();

        db.Enrollments.Add(new Enrollment
        {
            StudentProfileId = profile.Id,
            RequestedCategory = model.LicenseCategory
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] =
            "Курсистът е създаден и заявката му е добавена за преглед.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        await LoadInstructorsAsync();

        return View(new StudentEditViewModel
        {
            Id = profile.Id,
            FullName = profile.ApplicationUser.FullName,
            LicenseCategory = profile.LicenseCategory,
            ProgressPercent = profile.ProgressPercent,
            InstructorProfileId = profile.InstructorProfileId,
            IsActive = profile.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadInstructorsAsync();
            return View(model);
        }

        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == model.Id);

        if (profile is null)
            return NotFound();

        if (model.InstructorProfileId.HasValue)
        {
            var instructorExists = await db.InstructorProfiles.AnyAsync(item =>
                item.Id == model.InstructorProfileId.Value && item.IsActive);

            if (!instructorExists)
            {
                ModelState.AddModelError(
                    nameof(model.InstructorProfileId),
                    "Избраният инструктор не съществува или не е активен.");

                await LoadInstructorsAsync();
                return View(model);
            }
        }

        profile.ApplicationUser.FullName = model.FullName.Trim();
        profile.LicenseCategory = model.LicenseCategory.Trim().ToUpperInvariant();
        profile.ProgressPercent = model.ProgressPercent;
        profile.InstructorProfileId = model.InstructorProfileId;
        profile.IsActive = model.IsActive;

        if (!model.IsActive)
        {
            profile.ApplicationUser.LockoutEnabled = true;
            profile.ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else
        {
            profile.ApplicationUser.LockoutEnd = null;
        }

        await db.SaveChangesAsync();

        TempData["Success"] = "Профилът на курсиста е обновен.";

        return RedirectToAction(nameof(Index));
    }

    // Деактивиране на акаунт
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        profile.IsActive = false;
        profile.ApplicationUser.LockoutEnabled = true;
        profile.ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue;

        await db.SaveChangesAsync();

        TempData["Success"] = "Акаунтът на курсиста е деактивиран.";

        return RedirectToAction(nameof(Index));
    }

    // Активиране на акаунт
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        profile.IsActive = true;
        profile.ApplicationUser.LockoutEnd = null;

        await db.SaveChangesAsync();

        TempData["Success"] = "Акаунтът на курсиста е активиран.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        ViewBag.HasHistory =
            await db.LessonBookings.AnyAsync(item => item.StudentProfileId == id) ||
            await db.Payments.AnyAsync(item => item.StudentProfileId == id) ||
            await db.TestResults.AnyAsync(item => item.StudentProfileId == id);

        return View(profile);
    }

    // Окончателно изтриване на акаунт
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var testResults = await db.TestResults
                .Where(item => item.StudentProfileId == id)
                .ToListAsync();

            db.TestResults.RemoveRange(testResults);

            var bookings = await db.LessonBookings
                .Where(item => item.StudentProfileId == id)
                .ToListAsync();

            db.LessonBookings.RemoveRange(bookings);

            var enrollments = await db.Enrollments
                .Where(item => item.StudentProfileId == id)
                .ToListAsync();

            db.Enrollments.RemoveRange(enrollments);

            var payments = await db.Payments
                .Where(item => item.StudentProfileId == id)
                .ToListAsync();

            db.Payments.RemoveRange(payments);

            var notifications = await db.Notifications
                .Where(item => item.ApplicationUserId == profile.ApplicationUserId)
                .ToListAsync();

            db.Notifications.RemoveRange(notifications);

            await db.SaveChangesAsync();

            var result = await userManager.DeleteAsync(profile.ApplicationUser);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["Error"] = string.Join(
                    " ",
                    result.Errors.Select(error => error.Description));

                return RedirectToAction(nameof(Index));
            }

            await transaction.CommitAsync();

            TempData["Success"] = "Акаунтът на курсиста е изтрит окончателно.";
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["Error"] =
                "Възникна грешка при изтриването. Данните не бяха изтрити.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadInstructorsAsync()
    {
        ViewBag.Instructors = await db.InstructorProfiles
            .Where(item => item.IsActive)
            .Include(item => item.ApplicationUser)
            .OrderBy(item => item.ApplicationUser.FullName)
            .ToListAsync();
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
