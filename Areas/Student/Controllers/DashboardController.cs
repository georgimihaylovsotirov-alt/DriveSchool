
using System.Security.Claims;
using System.Text.Json;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public sealed class DashboardController(
    DriveSchoolDbContext db,
    BookingService bookings,
    UserManager<ApplicationUser> userManager,
    DatabaseAvailability database) : Controller
{
    public async Task<IActionResult> Index()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        ViewBag.CompletedCount = await db.LessonBookings.CountAsync(item =>
            item.StudentProfileId == student.Id && item.Status == BookingStatus.Completed);
        ViewBag.UpcomingCount = await db.LessonBookings.CountAsync(item =>
            item.StudentProfileId == student.Id &&
            (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed) &&
            item.StartsAt >= DateTime.Now);
        ViewBag.LatestResults = await db.TestResults
            .Where(item => item.StudentProfileId == student.Id)
            .Include(item => item.TheoryTest)
            .OrderByDescending(item => item.TakenAt)
            .Take(3)
            .ToListAsync();
        ViewBag.Upcoming = await db.LessonBookings
            .Where(item => item.StudentProfileId == student.Id &&
                (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed) &&
                item.StartsAt >= DateTime.Now)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Car)
            .OrderBy(item => item.StartsAt)
            .Take(5)
            .ToListAsync();

        return View(student);
    }

    public async Task<IActionResult> MyInstructor()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var instructor = student.InstructorProfileId.HasValue
            ? await db.InstructorProfiles
                .Include(item => item.ApplicationUser)
                .FirstOrDefaultAsync(item => item.Id == student.InstructorProfileId)
            : null;

        return View(instructor);
    }

    [HttpGet]
    public async Task<IActionResult> Book()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var approved = await db.Enrollments.AnyAsync(item =>
            item.StudentProfileId == student.Id && item.Status == EnrollmentStatus.Approved);

        ViewBag.EnrollmentApproved = approved;

        if (!approved)
        {
            ViewBag.Slots = new List<LessonBooking>();
            return View();
        }

        var slotQuery = db.LessonBookings
            .Where(item => item.Status == BookingStatus.Available && item.StartsAt > DateTime.Now)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Car)
            .AsQueryable();

        if (student.InstructorProfileId.HasValue)
        {
            slotQuery = slotQuery.Where(item => item.InstructorProfileId == student.InstructorProfileId.Value);
        }

        ViewBag.Slots = await slotQuery.OrderBy(item => item.StartsAt).ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestLesson(int id, string? note)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        if (!await HasApprovedEnrollmentAsync(student.Id))
        {
            TempData["Error"] = "Можете да запазите урок след одобрение на заявката за обучение.";
            return RedirectToAction(nameof(Book));
        }

        var requestedSlot = await db.LessonBookings.FirstOrDefaultAsync(item =>
            item.Id == id &&
            item.Status == BookingStatus.Available &&
            (!student.InstructorProfileId.HasValue || item.InstructorProfileId == student.InstructorProfileId.Value));

        if (requestedSlot is null)
        {
            TempData["Error"] = "Избраният час не е наличен за вашия профил.";
            return RedirectToAction(nameof(Book));
        }

        var result = await bookings.RequestAsync(id, student.Id, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Book));
    }

    public async Task<IActionResult> Schedule()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var items = await db.LessonBookings
            .Where(item => item.StudentProfileId == student.Id)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Car)
            .OrderByDescending(item => item.StartsAt)
            .ToListAsync();

        return View(items);
    }

    public async Task<IActionResult> Lessons()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var items = await db.LessonBookings
            .Where(item => item.StudentProfileId == student.Id && item.Status == BookingStatus.Completed)
            .Include(item => item.InstructorProfile).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Lesson)!.ThenInclude(item => item!.Evaluation)
            .OrderByDescending(item => item.StartsAt)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var result = await bookings.CancelAsync(id, student.Id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Schedule));
    }

    public async Task<IActionResult> Progress()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        ViewBag.Results = await db.TestResults
            .Where(item => item.StudentProfileId == student.Id)
            .Include(item => item.TheoryTest)
            .OrderByDescending(item => item.TakenAt)
            .ToListAsync();

        var lessons = await db.LessonBookings
            .Where(item => item.StudentProfileId == student.Id && item.Status == BookingStatus.Completed)
            .Include(item => item.Lesson)!.ThenInclude(item => item!.Evaluation)
            .OrderByDescending(item => item.StartsAt)
            .ToListAsync();

        return View(lessons);
    }

    public async Task<IActionResult> TheoryTests()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        if (!await HasApprovedEnrollmentAsync(student.Id))
        {
            ViewBag.EnrollmentPending = true;
            ViewBag.Results = new List<TestResult>();

            return View(new List<TheoryTest>());
        }

        ViewBag.EnrollmentPending = false;

        ViewBag.Results = await db.TestResults
            .Where(item => item.StudentProfileId == student.Id)
            .Include(item => item.TheoryTest)
            .OrderByDescending(item => item.TakenAt)
            .Take(5)
            .ToListAsync();

        return View(await db.TheoryTests
            .Where(test => test.IsPublished)
            .OrderBy(test => test.Title)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> TakeTest(int id)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        if (!await HasApprovedEnrollmentAsync(student.Id))
        {
            TempData["Error"] = "Нямате достъп до тестовете. Изчакайте одобрение на записването си.";
            return RedirectToAction(nameof(TheoryTests));
        }

        if (!database.IsConfigured) return NotFound();

        var test = await db.TheoryTests
            .Where(item => item.Id == id && item.IsPublished)
            .Include(item => item.Questions).ThenInclude(item => item.Answers)
            .FirstOrDefaultAsync();

        if (test is null) return NotFound();

        HttpContext.Session.SetString($"test-start-{id}", DateTimeOffset.UtcNow.ToString("O"));

        return View(test);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTest(TheoryTestSubmissionViewModel model)
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        if (!await HasApprovedEnrollmentAsync(student.Id))
        {
            HttpContext.Session.Remove($"test-start-{model.TestId}");

            TempData["Error"] = "Нямате право да предавате тестове, преди записването ви да бъде одобрено.";

            return RedirectToAction(nameof(TheoryTests));
        }

        var test = await db.TheoryTests
            .Where(item => item.Id == model.TestId && item.IsPublished)
            .Include(item => item.Questions).ThenInclude(item => item.Answers)
            .FirstOrDefaultAsync();

        if (test is null) return NotFound();

        var startedAt = HttpContext.Session.GetString($"test-start-{model.TestId}");
        HttpContext.Session.Remove($"test-start-{model.TestId}");

        if (!DateTimeOffset.TryParse(startedAt, out var started) ||
            DateTimeOffset.UtcNow - started > TimeSpan.FromMinutes(test.TimeLimitMinutes + 1))
        {
            TempData["Error"] = "Времето за теста е изтекло или опитът не е започнат правилно.";
            return RedirectToAction(nameof(TheoryTests));
        }

        var validAnswers = test.Questions
            .SelectMany(question => question.Answers.Select(answer => new
            {
                QuestionId = question.Id,
                AnswerId = answer.Id,
                answer.IsCorrect
            }))
            .ToDictionary(answer => answer.AnswerId, answer => answer);

        var correct = 0;

        foreach (var question in test.Questions)
        {
            if (model.Answers.TryGetValue(question.Id, out var answerId) &&
                validAnswers.TryGetValue(answerId, out var answer) &&
                answer.QuestionId == question.Id && answer.IsCorrect &&
                question.Answers.Any(option => option.Id == answerId))
            {
                correct++;
            }
        }

        var score = test.Questions.Count == 0
            ? 0
            : (int)Math.Round(100d * correct / test.Questions.Count);

        db.TestResults.Add(new TestResult
        {
            TheoryTestId = test.Id,
            StudentProfileId = student.Id,
            Score = score,
            Passed = score >= test.PassingScore,
            AnswerSheet = JsonSerializer.Serialize(model.Answers),
            TakenAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        TempData["Success"] = $"Резултат: {score}%. {(score >= test.PassingScore ? "Издържахте теста." : "Не достигнахте минималния резултат.")}";

        return RedirectToAction(nameof(TheoryTests));
    }

    public async Task<IActionResult> Payments()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        var items = await db.Payments
            .Where(item => item.StudentProfileId == student.Id)
            .OrderByDescending(item => item.DueAt)
            .ToListAsync();

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var student = await CurrentStudentAsync();
        if (student is null) return NotFound();

        return View(new StudentProfileViewModel
        {
            FullName = student.ApplicationUser.FullName,
            DateOfBirth = student.DateOfBirth,
            LicenseCategory = student.LicenseCategory
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(StudentProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var student = await db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (student is null) return NotFound();

        student.ApplicationUser.FullName = model.FullName.Trim();
        student.DateOfBirth = model.DateOfBirth;
        student.LicenseCategory = model.LicenseCategory.Trim().ToUpperInvariant();

        var userResult = await userManager.UpdateAsync(student.ApplicationUser);

        if (!userResult.Succeeded)
        {
            foreach (var error in userResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await db.SaveChangesAsync();

        TempData["Success"] = "Личните данни са обновени.";

        return RedirectToAction(nameof(Profile));
    }

    private async Task<bool> HasApprovedEnrollmentAsync(int studentProfileId)
    {
        return await db.Enrollments.AnyAsync(item =>
            item.StudentProfileId == studentProfileId &&
            item.Status == EnrollmentStatus.Approved);
    }

    private Task<StudentProfile?> CurrentStudentAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return db.StudentProfiles
            .Include(item => item.ApplicationUser)
            .Include(item => item.Enrollments)
            .Include(item => item.InstructorProfile).ThenInclude(item => item!.ApplicationUser)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == userId && item.IsActive);
    }
}
