using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class SettingsController(
    DriveSchoolDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var accounts = await db.Users
            .OrderBy(user => user.FullName)
            .ToListAsync();
        var rows = new List<(ApplicationUser User, IList<string> Roles)>();
        foreach (var user in accounts)
        {
            rows.Add((user, await userManager.GetRolesAsync(user)));
        }
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (await userManager.IsInRoleAsync(user, AppRoles.Owner) && user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Не можете да блокирате собствения си профил.";
            return RedirectToAction(nameof(Index));
        }

        var locked = await userManager.IsLockedOutAsync(user);
        if (locked)
        {
            var studentDisabled = await db.StudentProfiles.AnyAsync(item => item.ApplicationUserId == id && !item.IsActive);
            var instructorDisabled = await db.InstructorProfiles.AnyAsync(item => item.ApplicationUserId == id && !item.IsActive);
            if (studentDisabled || instructorDisabled)
            {
                TempData["Error"] = "Първо активирайте профила в раздела за курсисти или инструктори.";
                return RedirectToAction(nameof(Index));
            }
        }
        user.LockoutEnabled = true;
        user.LockoutEnd = locked ? null : DateTimeOffset.MaxValue;
        var result = await userManager.UpdateAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? (locked ? "Профилът е отключен." : "Профилът е блокиран.")
            : "Промяната не беше записана.";
        return RedirectToAction(nameof(Index));
    }
}
