using System.Security.Claims;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class ProfileController(
    DriveSchoolDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentProfileAsync();
        if (profile is null) return NotFound();
        return View(new InstructorProfileViewModel
        {
            FullName = profile.ApplicationUser.FullName,
            PhoneNumber = profile.ApplicationUser.PhoneNumber,
            Bio = profile.Bio
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(InstructorProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var profile = await CurrentProfileAsync();
        if (profile is null) return NotFound();
        profile.ApplicationUser.FullName = model.FullName.Trim();
        profile.ApplicationUser.PhoneNumber = model.PhoneNumber?.Trim();
        profile.Bio = model.Bio.Trim();
        var result = await userManager.UpdateAsync(profile.ApplicationUser);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Профилът е обновен.";
        return RedirectToAction(nameof(Index));
    }

    private Task<InstructorProfile?> CurrentProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.InstructorProfiles.Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == userId && item.IsActive);
    }
}
