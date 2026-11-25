using System.ComponentModel.DataAnnotations;

namespace DriveSchool.Models;

public enum EnrollmentStatus
{
    Pending,
    Approved,
    Rejected
}

public enum InstructorApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum BookingStatus
{
    Available,
    Pending,
    Confirmed,
    Rejected,
    Cancelled,
    Completed
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Overdue
}

public enum TestCategory
{
    A,
    B,
    C,
    D
}

public sealed class StudentProfile
{
    public int Id { get; set; }
    [Required] public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    [StringLength(2)] public string LicenseCategory { get; set; } = "B";
    [Range(0, 100)] public int ProgressPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public int? InstructorProfileId { get; set; }
    public InstructorProfile? InstructorProfile { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<LessonBooking> Bookings { get; set; } = new List<LessonBooking>();
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public sealed class InstructorProfile
{
    public int Id { get; set; }
    [Required] public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    [StringLength(1200)] public string Bio { get; set; } = string.Empty;
    [Range(0, 60)] public int YearsExperience { get; set; }
    [StringLength(100)] public string Categories { get; set; } = "B";
    public bool IsActive { get; set; } = true;
    public InstructorApprovalStatus ApprovalStatus { get; set; } = InstructorApprovalStatus.Approved;
    [StringLength(30)] public string? PhoneNumber { get; set; }
    [StringLength(1200)] public string? CvOriginalFileName { get; set; }
    [StringLength(100)] public string? CvStoredFileName { get; set; }
    [StringLength(120)] public string TrainingCarMakeModel { get; set; } = string.Empty;
    [StringLength(20)] public string TrainingCarRegistrationNumber { get; set; } = string.Empty;
    [Range(1990, 2100)] public int? TrainingCarYear { get; set; } = DateTime.UtcNow.Year;
    [StringLength(10)] public string TrainingCarCategory { get; set; } = "B";
    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
    public ICollection<LessonBooking> Bookings { get; set; } = new List<LessonBooking>();
}

public sealed class Car
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string RegistrationNumber { get; set; } = string.Empty;
    [Required, StringLength(100)] public string MakeModel { get; set; } = string.Empty;
    [Required, StringLength(10)] public string Category { get; set; } = "B";
    [Range(1990, 2100)] public int Year { get; set; } = DateTime.UtcNow.Year;
    public bool IsAvailable { get; set; } = true;
    public ICollection<LessonBooking> Bookings { get; set; } = new List<LessonBooking>();
}

public sealed class Enrollment
{
    public int Id { get; set; }
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    [Required, StringLength(10)] public string RequestedCategory { get; set; } = "B";
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

public sealed class LessonBooking
{
    public int Id { get; set; }
    public int? StudentProfileId { get; set; }
    public StudentProfile? StudentProfile { get; set; }
    public int InstructorProfileId { get; set; }
    public InstructorProfile InstructorProfile { get; set; } = null!;
    public int? CarId { get; set; }
    public Car? Car { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Available;
    [StringLength(600)] public string? StudentNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Lesson? Lesson { get; set; }
}

public sealed class Lesson
{
    public int Id { get; set; }
    public int LessonBookingId { get; set; }
    public LessonBooking Booking { get; set; } = null!;
    [StringLength(1000)] public string Summary { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public Evaluation? Evaluation { get; set; }
}

public sealed class Evaluation
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    [Range(1, 5)] public int VehicleControl { get; set; }
    [Range(1, 5)] public int RoadAwareness { get; set; }
    [Range(1, 5)] public int Confidence { get; set; }
    [StringLength(1000)] public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TheoryTest
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Title { get; set; } = string.Empty;
    public TestCategory Category { get; set; } = TestCategory.B;
    [Range(1, 180)] public int TimeLimitMinutes { get; set; } = 30;
    [Range(1, 100)] public int PassingScore { get; set; } = 80;
    public bool IsPublished { get; set; } = true;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<TestResult> Results { get; set; } = new List<TestResult>();
}

public sealed class Question
{
    public int Id { get; set; }
    public int TheoryTestId { get; set; }
    public TheoryTest TheoryTest { get; set; } = null!;
    [Required, StringLength(800)] public string Text { get; set; } = string.Empty;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}

public sealed class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    [Required, StringLength(400)] public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public sealed class TestResult
{
    public int Id { get; set; }
    public int TheoryTestId { get; set; }
    public TheoryTest TheoryTest { get; set; } = null!;
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    [Range(0, 100)] public int Score { get; set; }
    public bool Passed { get; set; }
    [StringLength(4000)] public string AnswerSheet { get; set; } = "[]";
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
}

public sealed class Payment
{
    public int Id { get; set; }
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    [Required, StringLength(120)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)] public decimal Amount { get; set; }
    [StringLength(3)] public string Currency { get; set; } = "BGN";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? PaidAt { get; set; }
}

public sealed class Notification
{
    public int Id { get; set; }
    [Required] public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    [Required, StringLength(300)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
