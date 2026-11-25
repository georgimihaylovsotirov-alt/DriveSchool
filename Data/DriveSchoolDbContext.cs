using DriveSchool.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Data;

public sealed class DriveSchoolDbContext(DbContextOptions<DriveSchoolDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonBooking> LessonBookings => Set<LessonBooking>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<TheoryTest> TheoryTests => Set<TheoryTest>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().Property(user => user.FullName).HasMaxLength(120);
        builder.Entity<StudentProfile>().HasIndex(profile => profile.ApplicationUserId).IsUnique();
        builder.Entity<InstructorProfile>().HasIndex(profile => profile.ApplicationUserId).IsUnique();
        builder.Entity<InstructorProfile>()
            .Property(profile => profile.ApprovalStatus)
            .HasDefaultValue(InstructorApprovalStatus.Approved);
        builder.Entity<Car>().HasIndex(car => car.RegistrationNumber).IsUnique();
        builder.Entity<Lesson>().HasIndex(lesson => lesson.LessonBookingId).IsUnique();

        builder.Entity<StudentProfile>()
            .HasOne(profile => profile.ApplicationUser)
            .WithOne(user => user.StudentProfile)
            .HasForeignKey<StudentProfile>(profile => profile.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InstructorProfile>()
            .HasOne(profile => profile.ApplicationUser)
            .WithOne(user => user.InstructorProfile)
            .HasForeignKey<InstructorProfile>(profile => profile.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasOne(profile => profile.InstructorProfile)
            .WithMany(instructor => instructor.Students)
            .HasForeignKey(profile => profile.InstructorProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<LessonBooking>()
            .HasOne(booking => booking.StudentProfile)
            .WithMany(student => student.Bookings)
            .HasForeignKey(booking => booking.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LessonBooking>()
            .HasOne(booking => booking.InstructorProfile)
            .WithMany(instructor => instructor.Bookings)
            .HasForeignKey(booking => booking.InstructorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LessonBooking>()
            .HasOne(booking => booking.Car)
            .WithMany(car => car.Bookings)
            .HasForeignKey(booking => booking.CarId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LessonBooking>()
            .ToTable(table => table.HasCheckConstraint("CK_LessonBooking_TimeRange", "[EndsAt] > [StartsAt]"));

        builder.Entity<Lesson>()
            .HasOne(lesson => lesson.Booking)
            .WithOne(booking => booking.Lesson)
            .HasForeignKey<Lesson>(lesson => lesson.LessonBookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Evaluation>()
            .HasOne(evaluation => evaluation.Lesson)
            .WithOne(lesson => lesson.Evaluation)
            .HasForeignKey<Evaluation>(evaluation => evaluation.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>().Property(payment => payment.Amount).HasPrecision(10, 2);
        builder.Entity<LessonBooking>().HasIndex(booking => new { booking.InstructorProfileId, booking.StartsAt, booking.EndsAt });
        builder.Entity<LessonBooking>().HasIndex(booking => new { booking.StudentProfileId, booking.StartsAt, booking.EndsAt });
        builder.Entity<LessonBooking>().HasIndex(booking => new { booking.CarId, booking.StartsAt, booking.EndsAt });
        builder.Entity<Enrollment>().HasIndex(enrollment => new { enrollment.StudentProfileId, enrollment.Status });
        builder.Entity<TestResult>().HasIndex(result => new { result.StudentProfileId, result.TakenAt });
    }
}
