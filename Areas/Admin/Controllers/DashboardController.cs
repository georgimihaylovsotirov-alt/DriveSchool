using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class DashboardController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalStudents = await db.StudentProfiles.CountAsync();
        ViewBag.ActiveStudents = await db.StudentProfiles.CountAsync(student => student.IsActive);
        ViewBag.TotalInstructors = await db.InstructorProfiles.CountAsync(instructor =>
            instructor.IsActive && instructor.ApprovalStatus == InstructorApprovalStatus.Approved);
        ViewBag.PendingInstructorApplications = await db.InstructorProfiles.CountAsync(instructor =>
            instructor.ApprovalStatus == InstructorApprovalStatus.Pending);
        ViewBag.AvailableCars = await db.Cars.CountAsync(car => car.IsAvailable);
        ViewBag.PendingEnrollments = await db.Enrollments.CountAsync(item => item.Status == EnrollmentStatus.Pending);
        ViewBag.TodayLessons = await db.LessonBookings.CountAsync(item =>
            item.StartsAt.Date == DateTime.Today &&
            (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed));
        ViewBag.PendingBookings = await db.LessonBookings
            .Where(item => item.Status == BookingStatus.Pending)
            .Include(item => item.StudentProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .OrderBy(item => item.StartsAt)
            .Take(6)
            .ToListAsync();
        ViewBag.MonthIncome = await db.Payments
            .Where(item => item.Status == PaymentStatus.Paid &&
                item.PaidAt.HasValue &&
                item.PaidAt.Value.Month == DateTime.Today.Month &&
                item.PaidAt.Value.Year == DateTime.Today.Year)
            .SumAsync(item => (decimal?)item.Amount) ?? 0m;
        return View();
    }
}
