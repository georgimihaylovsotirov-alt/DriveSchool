using System.ComponentModel.DataAnnotations;
using DriveSchool.Models;
using Microsoft.AspNetCore.Http;

namespace DriveSchool.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Въведете имейл адрес.")]
    [EmailAddress(ErrorMessage = "Въведете валиден имейл адрес.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете парола.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Изберете тип на профила.")]
    [Display(Name = "Тип на профила")]
    public string AccountType { get; set; } = "Student";

    [Required(ErrorMessage = "Въведете име и фамилия.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 120 знака.")]
    [Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете имейл адрес.")]
    [EmailAddress(ErrorMessage = "Въведете валиден имейл адрес.")]
    [Display(Name = "Имейл адрес")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Изберете категория.")]
    [Display(Name = "Категория за обучение")]
    public string LicenseCategory { get; set; } = "B";

    [Phone(ErrorMessage = "Въведете валиден телефонен номер.")]
    [StringLength(30)]
    [Display(Name = "Телефон за връзка")]
    public string? PhoneNumber { get; set; }

    [StringLength(1200, ErrorMessage = "Представянето не може да е по-дълго от 1200 знака.")]
    [Display(Name = "Професионално представяне")]
    public string Bio { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Годините опит трябва да са между 0 и 60.")]
    [Display(Name = "Години професионален опит")]
    public int YearsExperience { get; set; }

    [StringLength(100)]
    [Display(Name = "Категории, по които можете да обучавате (напр. B, A)")]
    public string Categories { get; set; } = "B";

    [StringLength(120)]
    [Display(Name = "Марка и модел на обучителния автомобил")]
    public string TrainingCarMakeModel { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Регистрационен номер")]
    public string TrainingCarRegistrationNumber { get; set; } = string.Empty;

    [Range(1990, 2100, ErrorMessage = "Въведете валидна година на производство.")]
    [Display(Name = "Година на автомобила")]
    public int TrainingCarYear { get; set; } = DateTime.UtcNow.Year;

    [Display(Name = "Категория на обучителния автомобил")]
    public string TrainingCarCategory { get; set; } = "B";

    [Display(Name = "CV файл (PDF, DOC или DOCX)")]
    public IFormFile? CvFile { get; set; }

    [Required(ErrorMessage = "Въведете парола.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Паролата трябва да е поне 10 знака.")]
    [DataType(DataType.Password)]
    [Display(Name = "Парола")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
    [Display(Name = "Повторете паролата")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class CompleteLessonViewModel
{
    public int BookingId { get; set; }
    [Required, StringLength(1000, MinimumLength = 3, ErrorMessage = "Добавете кратко резюме на урока.")]
    [Display(Name = "Резюме на урока")]
    public string Summary { get; set; } = string.Empty;
    [Range(1, 5), Display(Name = "Контрол над автомобила")]
    public int VehicleControl { get; set; } = 3;
    [Range(1, 5), Display(Name = "Ориентация на пътя")]
    public int RoadAwareness { get; set; } = 3;
    [Range(1, 5), Display(Name = "Увереност")]
    public int Confidence { get; set; } = 3;
    [StringLength(1000), Display(Name = "Коментар към курсиста")]
    public string Comment { get; set; } = string.Empty;
}

public sealed class TheoryTestSubmissionViewModel
{
    public int TestId { get; set; }
    public Dictionary<int, int> Answers { get; set; } = new();
}

public sealed class ContactViewModel
{
    [Required, StringLength(120), Display(Name = "Вашето име")]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, Display(Name = "Имейл адрес")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 10), Display(Name = "Съобщение")]
    public string Message { get; set; } = string.Empty;
}

public sealed class BookingSlotViewModel
{
    [Required, Display(Name = "Начало")]
    public DateTime StartsAt { get; set; }
    [Required, Display(Name = "Край")]
    public DateTime EndsAt { get; set; }
    [Display(Name = "Автомобил")]
    public int? CarId { get; set; }
}

public sealed class StudentCreateViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, Display(Name = "Имейл адрес")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 10), Display(Name = "Временна парола")]
    public string Password { get; set; } = string.Empty;
    [Required, Display(Name = "Категория")]
    public string LicenseCategory { get; set; } = "B";
    [Display(Name = "Инструктор")]
    public int? InstructorProfileId { get; set; }
}

public sealed class StudentEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [Required, StringLength(10), Display(Name = "Категория")]
    public string LicenseCategory { get; set; } = "B";
    [Range(0, 100), Display(Name = "Напредък (%)")]
    public int ProgressPercent { get; set; }
    [Display(Name = "Инструктор")]
    public int? InstructorProfileId { get; set; }
    [Display(Name = "Активен профил")]
    public bool IsActive { get; set; } = true;
}

public sealed class InstructorCreateViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, Display(Name = "Имейл адрес")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 10), Display(Name = "Временна парола")]
    public string Password { get; set; } = string.Empty;
    [StringLength(1200), Display(Name = "Представяне")]
    public string Bio { get; set; } = string.Empty;
    [Range(0, 60), Display(Name = "Години опит")]
    public int YearsExperience { get; set; }
    [Required, StringLength(100), Display(Name = "Категории")]
    public string Categories { get; set; } = "B";
    public bool IsActive { get; set; } = true;
}

public sealed class InstructorEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [StringLength(1200), Display(Name = "Представяне")]
    public string Bio { get; set; } = string.Empty;
    [Range(0, 60), Display(Name = "Години опит")]
    public int YearsExperience { get; set; }
    [Required, StringLength(100), Display(Name = "Категории")]
    public string Categories { get; set; } = "B";
    [Display(Name = "Активен профил")]
    public bool IsActive { get; set; } = true;
}

public sealed class CarEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Регистрационен номер")]
    public string RegistrationNumber { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Марка и модел")]
    public string MakeModel { get; set; } = string.Empty;
    [Required, StringLength(10), Display(Name = "Категория")]
    public string Category { get; set; } = "B";
    [Range(1990, 2100), Display(Name = "Година")]
    public int Year { get; set; } = DateTime.UtcNow.Year;
    [Display(Name = "Активен")]
    public bool IsAvailable { get; set; } = true;
}

public sealed class PaymentCreateViewModel
{
    [Required, Display(Name = "Курсист")]
    public int StudentProfileId { get; set; }
    [Required, StringLength(120), Display(Name = "Основание")]
    public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true), Display(Name = "Сума")]
    public decimal Amount { get; set; }
    [DataType(DataType.Date), Display(Name = "Падеж")]
    public DateTime DueAt { get; set; } = DateTime.Today.AddDays(14);
}

public sealed class EnrollmentReviewViewModel
{
    public int Id { get; set; }
    [Required, Display(Name = "Решение")]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Approved;
    [Display(Name = "Инструктор")]
    public int? InstructorProfileId { get; set; }
}

public sealed class TheoryTestCreateViewModel
{
    public int Id { get; set; }
    [Required, StringLength(120), Display(Name = "Наименование")]
    public string Title { get; set; } = string.Empty;
    [Display(Name = "Категория")]
    public TestCategory Category { get; set; } = TestCategory.B;
    [Range(1, 180), Display(Name = "Време в минути")]
    public int TimeLimitMinutes { get; set; } = 30;
    [Range(1, 100), Display(Name = "Минимален резултат (%)")]
    public int PassingScore { get; set; } = 80;
    [Display(Name = "Публикуван")]
    public bool IsPublished { get; set; }
}

public sealed class TheoryQuestionCreateViewModel
{
    public int TheoryTestId { get; set; }
    [Required, StringLength(800), Display(Name = "Въпрос")]
    public string Text { get; set; } = string.Empty;
    [Required, StringLength(400), Display(Name = "Отговор 1")]
    public string Answer1 { get; set; } = string.Empty;
    [Required, StringLength(400), Display(Name = "Отговор 2")]
    public string Answer2 { get; set; } = string.Empty;
    [Required, StringLength(400), Display(Name = "Отговор 3")]
    public string Answer3 { get; set; } = string.Empty;
    [Required, StringLength(400), Display(Name = "Отговор 4")]
    public string Answer4 { get; set; } = string.Empty;
    [Range(1, 4), Display(Name = "Номер на верния отговор")]
    public int CorrectAnswer { get; set; } = 1;
}

public sealed class StudentProfileViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [DataType(DataType.Date), Display(Name = "Дата на раждане")]
    public DateTime? DateOfBirth { get; set; }
    [Required, StringLength(10), Display(Name = "Категория")]
    public string LicenseCategory { get; set; } = "B";
}

public sealed class InstructorProfileViewModel
{
    [Required, StringLength(120, MinimumLength = 2), Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(30), Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }
    [StringLength(1200), Display(Name = "Представяне")]
    public string Bio { get; set; } = string.Empty;
}
