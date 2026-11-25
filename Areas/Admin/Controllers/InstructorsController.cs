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
public sealed class InstructorsController(
    DriveSchoolDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .Include(item => item.Students)
            .OrderBy(item => item.ApprovalStatus)
            .ThenBy(item => item.ApplicationUser.FullName)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new InstructorCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InstructorCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
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
            return View(model);
        }
        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Instructor);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(roleResult);
            return View(model);
        }
        db.InstructorProfiles.Add(new InstructorProfile
        {
            ApplicationUserId = user.Id,
            Bio = model.Bio.Trim(),
            YearsExperience = model.YearsExperience,
            Categories = model.Categories.Trim().ToUpperInvariant(),
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Профилът на инструктора е създаден.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await db.InstructorProfiles.Include(item => item.ApplicationUser).FirstOrDefaultAsync(item => item.Id == id);
        if (profile is null) return NotFound();
        return View(new InstructorEditViewModel
        {
            Id = profile.Id,
            FullName = profile.ApplicationUser.FullName,
            Bio = profile.Bio,
            YearsExperience = profile.YearsExperience,
            Categories = profile.Categories,
            IsActive = profile.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InstructorEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var profile = await db.InstructorProfiles.Include(item => item.ApplicationUser).FirstOrDefaultAsync(item => item.Id == model.Id);
        if (profile is null) return NotFound();
        profile.ApplicationUser.FullName = model.FullName.Trim();
        profile.Bio = model.Bio.Trim();
        profile.YearsExperience = model.YearsExperience;
        profile.Categories = model.Categories.Trim().ToUpperInvariant();
        profile.IsActive = model.IsActive;
        if (model.IsActive)
            profile.ApprovalStatus = InstructorApprovalStatus.Approved;

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
        TempData["Success"] = "Данните на инструктора са обновени.";
        return RedirectToAction(nameof(Index));
    }


    // Деактивиране на акаунт на инструктор
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        profile.IsActive = false;
        profile.ApplicationUser.LockoutEnabled = true;
        profile.ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue;

        await db.SaveChangesAsync();

        TempData["Success"] = "Акаунтът на инструктора е деактивиран.";

        return RedirectToAction(nameof(Index));
    }

    // Активиране на акаунт на инструктор
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        profile.IsActive = true;
        profile.ApprovalStatus = InstructorApprovalStatus.Approved;
        profile.ApplicationUser.LockoutEnd = null;

        await db.SaveChangesAsync();

        TempData["Success"] = "Акаунтът на инструктора е активиран.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        if (profile.ApprovalStatus != InstructorApprovalStatus.Pending)
        {
            TempData["Error"] = "Само чакащи заявки могат да бъдат одобрявани.";
            return RedirectToAction(nameof(Index));
        }

        profile.ApprovalStatus = InstructorApprovalStatus.Approved;
        profile.IsActive = true;
        profile.ApplicationUser.LockoutEnd = null;

        await db.SaveChangesAsync();

        TempData["Success"] = $"Заявката на {profile.ApplicationUser.FullName} е одобрена. Инструкторът вече може да влиза в системата.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        if (profile.ApprovalStatus != InstructorApprovalStatus.Pending)
        {
            TempData["Error"] = "Само чакащи заявки могат да бъдат отказвани.";
            return RedirectToAction(nameof(Index));
        }

        profile.ApprovalStatus = InstructorApprovalStatus.Rejected;
        profile.IsActive = false;
        profile.ApplicationUser.LockoutEnabled = true;
        profile.ApplicationUser.LockoutEnd = DateTimeOffset.MaxValue;

        await db.SaveChangesAsync();

        TempData["Success"] = $"Заявката на {profile.ApplicationUser.FullName} е отказана.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCv(int id)
    {
        var profile = await db.InstructorProfiles
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null || string.IsNullOrWhiteSpace(profile.CvStoredFileName))
            return NotFound();

        var safeStoredName = Path.GetFileName(profile.CvStoredFileName);
        var filePath = Path.Combine(
            environment.ContentRootPath, "App_Data", "InstructorCVs", safeStoredName);

        if (!System.IO.File.Exists(filePath))
            return NotFound("CV файлът не е намерен.");

        var extension = Path.GetExtension(safeStoredName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType,
            profile.CvOriginalFileName ?? "Instructor-CV" + extension);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        ViewBag.AssignedStudents = await db.StudentProfiles
            .CountAsync(item => item.InstructorProfileId == id);

        ViewBag.HasBookings = await db.LessonBookings
            .AnyAsync(item => item.InstructorProfileId == id &&
                (item.StudentProfileId != null || item.Status != BookingStatus.Available));

        return View(profile);
    }

    // Окончателно изтриване на инструктор
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await db.InstructorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (profile is null)
            return NotFound();

        var assignedStudents = await db.StudentProfiles
            .CountAsync(item => item.InstructorProfileId == id);

        var hasBookings = await db.LessonBookings
            .AnyAsync(item => item.InstructorProfileId == id &&
                (item.StudentProfileId != null || item.Status != BookingStatus.Available));

        if (assignedStudents > 0 || hasBookings)
        {
            TempData["Error"] =
                "Инструкторът не може да бъде изтрит, защото има назначени курсисти или история на уроци. Първо премахнете назначенията и проверете графика му.";

            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            // Премахват се само свободните часове, които нямат курсист.
            var availableBookings = await db.LessonBookings
                .Where(item => item.InstructorProfileId == id &&
                    item.StudentProfileId == null &&
                    item.Status == BookingStatus.Available)
                .ToListAsync();

            db.LessonBookings.RemoveRange(availableBookings);

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

            TempData["Success"] = "Акаунтът на инструктора е изтрит окончателно.";
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["Error"] =
                "Възникна грешка при изтриването. Данните не бяха изтрити.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}
