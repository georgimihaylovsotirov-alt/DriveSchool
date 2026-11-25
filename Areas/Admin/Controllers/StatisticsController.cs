using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class StatisticsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthStarts = Enumerable.Range(0, 6)
            .Select(offset => firstOfMonth.AddMonths(-5 + offset))
            .ToList();
        var completedLessons = await db.LessonBookings
            .Where(item => item.Status == BookingStatus.Completed && item.StartsAt >= monthStarts[0])
            .GroupBy(item => new { item.StartsAt.Year, item.StartsAt.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
            .ToListAsync();
        var revenue = await db.Payments
            .Where(item => item.Status == PaymentStatus.Paid && item.PaidAt >= monthStarts[0])
            .GroupBy(item => new { item.PaidAt!.Value.Year, item.PaidAt.Value.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(payment => payment.Amount) })
            .ToListAsync();
        ViewBag.Months = monthStarts.Select(month => new
        {
            Label = month.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("bg-BG")),
            Lessons = completedLessons.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Count ?? 0,
            Revenue = revenue.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Amount ?? 0m
        }).ToList();
        ViewBag.TotalStudents = await db.StudentProfiles.CountAsync(item => item.IsActive);
        ViewBag.PassRate = (await db.TestResults.CountAsync()) == 0
            ? 0
            : (int)Math.Round(100d * await db.TestResults.CountAsync(item => item.Passed) / await db.TestResults.CountAsync());
        ViewBag.Outstanding = await db.Payments
            .Where(item => item.Status != PaymentStatus.Paid)
            .SumAsync(item => (decimal?)item.Amount) ?? 0m;
        return View();
    }
}
