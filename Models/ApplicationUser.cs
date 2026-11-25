using Microsoft.AspNetCore.Identity;

namespace DriveSchool.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public StudentProfile? StudentProfile { get; set; }
    public InstructorProfile? InstructorProfile { get; set; }
}

public static class AppRoles
{
    public const string Owner = "Собственик";
    public const string Instructor = "Инструктор";
    public const string Student = "Курсист";

    public static readonly string[] All = [Owner, Instructor, Student];
}
