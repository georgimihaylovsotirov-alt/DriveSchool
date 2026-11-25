using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class PaymentsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Payments
            .Include(item => item.StudentProfile).ThenInclude(item => item.ApplicationUser)
            .OrderByDescending(item => item.DueAt)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadStudentsAsync();
        return View(new PaymentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();
            return View(model);
        }
        var studentExists = await db.StudentProfiles.AnyAsync(item => item.Id == model.StudentProfileId && item.IsActive);
        if (!studentExists)
        {
            ModelState.AddModelError(nameof(model.StudentProfileId), "Изберете активен курсист.");
            await LoadStudentsAsync();
            return View(model);
        }
        db.Payments.Add(new Payment
        {
            StudentProfileId = model.StudentProfileId,
            Description = model.Description.Trim(),
            Amount = model.Amount,
            DueAt = model.DueAt.Date,
            Currency = "BGN",
            Status = PaymentStatus.Pending
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Задължението е добавено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var payment = await db.Payments.FindAsync(id);
        if (payment is null) return NotFound();
        if (payment.Status != PaymentStatus.Paid)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        TempData["Success"] = "Плащането е отбелязано като получено.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStudentsAsync()
    {
        ViewBag.Students = await db.StudentProfiles
            .Where(item => item.IsActive)
            .Include(item => item.ApplicationUser)
            .OrderBy(item => item.ApplicationUser.FullName)
            .ToListAsync();
    }
}
