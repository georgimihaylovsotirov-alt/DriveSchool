using System.Diagnostics;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Controllers;

[AllowAnonymous]
public sealed class HomeController(DriveSchoolDbContext db, DatabaseAvailability database) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Instructors = database.IsConfigured
            ? await db.InstructorProfiles
                .Where(profile => profile.IsActive)
                .Include(profile => profile.ApplicationUser)
                .OrderByDescending(profile => profile.YearsExperience)
                .Take(3)
                .ToListAsync()
            : new List<InstructorProfile>();
        return View();
    }

    public IActionResult About() => View();

    public IActionResult Training() => View();

    public async Task<IActionResult> Instructors()
    {
        var instructors = database.IsConfigured
            ? await db.InstructorProfiles
                .Where(profile => profile.IsActive)
                .Include(profile => profile.ApplicationUser)
                .OrderBy(profile => profile.ApplicationUser.FullName)
                .ToListAsync()
            : new List<InstructorProfile>();
        return View(instructors);
    }

    [HttpGet]
    public IActionResult Contact() => View(new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!database.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Формата ще бъде активна след свързване на базата данни.");
            return View(model);
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ownerId = await (from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where role.Name == AppRoles.Owner
            select user.Id).FirstOrDefaultAsync();
        if (ownerId is null)
        {
            ModelState.AddModelError(string.Empty, "В момента не можем да приемем съобщението. Опитайте отново по-късно.");
            return View(model);
        }

        db.Notifications.Add(new Notification
        {
            ApplicationUserId = ownerId,
            Message = $"Съобщение от {model.Name} ({model.Email}): {model.Message.Trim()}"
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Съобщението е изпратено. Ще се свържем с вас скоро.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
