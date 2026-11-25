using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Controllers;

[Authorize]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    DriveSchoolDbContext db,
    DatabaseAvailability database,
    IWebHostEnvironment environment) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!database.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Входът ще бъде активен след свързване на Microsoft SQL Server.");
            return View(model);
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var candidate = await userManager.FindByEmailAsync(model.Email.Trim());
        if (candidate is not null && await userManager.IsInRoleAsync(candidate, AppRoles.Instructor))
        {
            var instructorProfile = await db.InstructorProfiles
                .FirstOrDefaultAsync(profile => profile.ApplicationUserId == candidate.Id);

            if (instructorProfile is null ||
                instructorProfile.ApprovalStatus == InstructorApprovalStatus.Pending)
            {
                ModelState.AddModelError(string.Empty,
                    "Заявката ви за инструкторски профил все още очаква одобрение от администратор.");
                return View(model);
            }

            if (instructorProfile.ApprovalStatus == InstructorApprovalStatus.Rejected)
            {
                ModelState.AddModelError(string.Empty,
                    "Заявката ви за инструкторски профил не е одобрена. Свържете се с администратора на школата.");
                return View(model);
            }

            if (!instructorProfile.IsActive)
            {
                ModelState.AddModelError(string.Empty,
                    "Достъпът до този профил е спрян. Свържете се с администратора на школата.");
                return View(model);
            }
        }

        if (candidate is not null &&
            await userManager.IsInRoleAsync(candidate, AppRoles.Student) &&
            !await db.StudentProfiles.AnyAsync(profile =>
                profile.ApplicationUserId == candidate.Id && profile.IsActive))
        {
            ModelState.AddModelError(string.Empty,
                "Достъпът до този профил е спрян. Свържете се с администратора на школата.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email.Trim(),
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }
            return await RedirectToRoleDashboardAsync(candidate);
        }

        ModelState.AddModelError(string.Empty,
            result.IsLockedOut ? "Профилът е временно заключен. Опитайте отново след 15 минути." : "Невалиден имейл или парола.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!database.IsConfigured)
        {
            ModelState.AddModelError(string.Empty,
                "Регистрацията ще бъде активна след свързване на Microsoft SQL Server.");
            return View(model);
        }

        var isInstructor = string.Equals(model.AccountType, "Instructor",
            StringComparison.OrdinalIgnoreCase);

        if (!isInstructor && !string.Equals(model.AccountType, "Student",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.AccountType), "Изберете валиден тип на профила.");
        }

        string[] allowedCategories = ["A", "B", "C", "D"];

        if (isInstructor)
        {
            var categories = (model.Categories ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToUpperInvariant())
                .Distinct()
                .ToArray();

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                ModelState.AddModelError(nameof(model.PhoneNumber), "Въведете телефон за връзка.");

            if (string.IsNullOrWhiteSpace(model.Bio) || model.Bio.Trim().Length < 10)
                ModelState.AddModelError(nameof(model.Bio), "Добавете професионално представяне (поне 10 знака).");

            if (categories.Length == 0 || categories.Any(value => !allowedCategories.Contains(value)))
                ModelState.AddModelError(nameof(model.Categories), "Посочете валидни категории: A, B, C или D.");

            if (string.IsNullOrWhiteSpace(model.TrainingCarMakeModel))
                ModelState.AddModelError(nameof(model.TrainingCarMakeModel), "Въведете марка и модел на автомобила.");

            if (string.IsNullOrWhiteSpace(model.TrainingCarRegistrationNumber))
                ModelState.AddModelError(nameof(model.TrainingCarRegistrationNumber), "Въведете регистрационен номер.");

            if (!allowedCategories.Contains(model.TrainingCarCategory?.ToUpperInvariant() ?? string.Empty))
                ModelState.AddModelError(nameof(model.TrainingCarCategory), "Изберете валидна категория на автомобила.");

            if (!categories.Contains(model.TrainingCarCategory?.ToUpperInvariant() ?? string.Empty))
                ModelState.AddModelError(nameof(model.TrainingCarCategory),
                    "Категорията на автомобила трябва да присъства сред категориите, по които обучавате.");

            if (model.CvFile is null || model.CvFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.CvFile), "Качването на CV файл е задължително.");
            }
            else
            {
                var extension = Path.GetExtension(model.CvFile.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError(nameof(model.CvFile), "CV файлът трябва да е PDF, DOC или DOCX.");

                if (model.CvFile.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError(nameof(model.CvFile), "Максималният размер на CV файла е 5 MB.");
            }
        }
        else if (!allowedCategories.Contains(model.LicenseCategory?.ToUpperInvariant() ?? string.Empty))
        {
            ModelState.AddModelError(nameof(model.LicenseCategory), "Изберете валидна категория.");
        }

        if (!ModelState.IsValid)
            return View(model);

        await using var transaction = await db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            PhoneNumber = isInstructor ? model.PhoneNumber?.Trim() : null
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var role = isInstructor ? AppRoles.Instructor : AppRoles.Student;
        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult);
            return View(model);
        }

        if (isInstructor)
        {
            var extension = Path.GetExtension(model.CvFile!.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var cvDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "InstructorCVs");
            Directory.CreateDirectory(cvDirectory);
            var cvPath = Path.Combine(cvDirectory, storedFileName);

            try
            {
                await using (var fileStream = new FileStream(
                    cvPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await model.CvFile.CopyToAsync(fileStream);
                }

                var instructor = new InstructorProfile
                {
                    ApplicationUserId = user.Id,
                    Bio = model.Bio.Trim(),
                    YearsExperience = model.YearsExperience,
                    Categories = string.Join(", ",
                        model.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries)
                            .Select(value => value.ToUpperInvariant()).Distinct()),
                    IsActive = false,
                    ApprovalStatus = InstructorApprovalStatus.Pending,
                    PhoneNumber = model.PhoneNumber?.Trim(),
                    CvOriginalFileName = Path.GetFileName(model.CvFile.FileName),
                    CvStoredFileName = storedFileName,
                    TrainingCarMakeModel = model.TrainingCarMakeModel.Trim(),
                    TrainingCarRegistrationNumber = model.TrainingCarRegistrationNumber.Trim().ToUpperInvariant(),
                    TrainingCarYear = model.TrainingCarYear,
                    TrainingCarCategory = model.TrainingCarCategory.ToUpperInvariant()
                };

                db.InstructorProfiles.Add(instructor);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Заявката ви за инструкторски профил е изпратена успешно. Ще получите достъп след одобрение от администратор.";
                return RedirectToAction(nameof(Login));
            }
            catch
            {
                if (System.IO.File.Exists(cvPath))
                    System.IO.File.Delete(cvPath);

                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty,
                    "Възникна проблем при изпращането на заявката. Проверете данните и опитайте отново.");
                return View(model);
            }
        }

        var student = new StudentProfile
        {
            ApplicationUserId = user.Id,
            LicenseCategory = model.LicenseCategory
        };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        db.Enrollments.Add(new Enrollment
        {
            StudentProfileId = student.Id,
            RequestedCategory = model.LicenseCategory,
            Status = EnrollmentStatus.Pending
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        await signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] =
            "Профилът е създаден. Заявката за обучение очаква потвърждение от школата.";
        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<IActionResult> RedirectToRoleDashboardAsync(ApplicationUser? user)
    {
        if (user is null)
        {
            return RedirectToAction("Index", "Home");
        }
        if (await userManager.IsInRoleAsync(user, AppRoles.Owner))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        if (await userManager.IsInRoleAsync(user, AppRoles.Instructor))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Instructor" });
        }
        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
