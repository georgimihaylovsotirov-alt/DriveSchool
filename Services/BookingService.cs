using System.Data;
using DriveSchool.Data;
using DriveSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Services;

public sealed class BookingService(DriveSchoolDbContext db)
{
    private static readonly BookingStatus[] OccupyingStatuses =
    [
        BookingStatus.Available,
        BookingStatus.Pending,
        BookingStatus.Confirmed
    ];

    public async Task<(bool Success, string Message)> CreateAvailabilityAsync(
        int instructorId,
        int? carId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken cancellationToken = default)
    {
        if (startsAt <= DateTime.Now || endsAt <= startsAt || endsAt - startsAt > TimeSpan.FromHours(3))
        {
            return (false, "Изберете бъдещ час с продължителност до три часа.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var collision = await db.LessonBookings.AnyAsync(booking =>
            OccupyingStatuses.Contains(booking.Status) &&
            booking.StartsAt < endsAt && startsAt < booking.EndsAt &&
            (booking.InstructorProfileId == instructorId || (carId.HasValue && booking.CarId == carId)),
            cancellationToken);
        if (collision)
        {
            return (false, "Инструкторът или автомобилът вече е зает в този период.");
        }

        db.LessonBookings.Add(new LessonBooking
        {
            InstructorProfileId = instructorId,
            CarId = carId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = BookingStatus.Available
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Свободният час е добавен.");
    }

    public async Task<(bool Success, string Message)> RequestAsync(
        int bookingId,
        int studentId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var booking = await db.LessonBookings
            .Include(item => item.InstructorProfile)
            .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Available || booking.StartsAt <= DateTime.Now)
        {
            return (false, "Този час вече не е свободен.");
        }

        var overlaps = await db.LessonBookings.AnyAsync(item =>
            item.Id != bookingId &&
            item.StudentProfileId == studentId &&
            (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed) &&
            item.StartsAt < booking.EndsAt && booking.StartsAt < item.EndsAt,
            cancellationToken);
        if (overlaps)
        {
            return (false, "Вече имате урок, който се застъпва с този час.");
        }

        booking.StudentProfileId = studentId;
        booking.Status = BookingStatus.Pending;
        booking.StudentNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        db.Notifications.Add(new Notification
        {
            ApplicationUserId = booking.InstructorProfile.ApplicationUserId,
            Message = "Имате нова заявка за урок."
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Заявката е изпратена към инструктора.");
    }

    public async Task<(bool Success, string Message)> ReviewRequestAsync(
        int bookingId,
        int instructorId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var booking = await db.LessonBookings
            .Include(item => item.StudentProfile)
            .FirstOrDefaultAsync(item => item.Id == bookingId && item.InstructorProfileId == instructorId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Pending || booking.StudentProfileId is null)
        {
            return (false, "Заявката вече не е активна.");
        }

        if (approve)
        {
            var instructorConflict = await db.LessonBookings.AnyAsync(item =>
                item.Id != bookingId &&
                item.InstructorProfileId == instructorId &&
                OccupyingStatuses.Contains(item.Status) &&
                item.StartsAt < booking.EndsAt && booking.StartsAt < item.EndsAt,
                cancellationToken);
            var carConflict = booking.CarId.HasValue && await db.LessonBookings.AnyAsync(item =>
                item.Id != bookingId &&
                item.CarId == booking.CarId &&
                OccupyingStatuses.Contains(item.Status) &&
                item.StartsAt < booking.EndsAt && booking.StartsAt < item.EndsAt,
                cancellationToken);
            var studentConflict = await db.LessonBookings.AnyAsync(item =>
                item.Id != bookingId &&
                item.StudentProfileId == booking.StudentProfileId &&
                (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Confirmed) &&
                item.StartsAt < booking.EndsAt && booking.StartsAt < item.EndsAt,
                cancellationToken);
            if (instructorConflict || carConflict || studentConflict)
            {
                return (false, "Часът вече се застъпва с друг урок. Изберете друго време.");
            }
        }

        booking.Status = approve ? BookingStatus.Confirmed : BookingStatus.Rejected;
        var user = await db.StudentProfiles
            .Where(student => student.Id == booking.StudentProfileId)
            .Select(student => student.ApplicationUserId)
            .SingleAsync(cancellationToken);
        db.Notifications.Add(new Notification
        {
            ApplicationUserId = user,
            Message = approve ? "Заявката ви за урок е потвърдена." : "Заявката ви за урок е отказана."
        });
        if (!approve)
        {
            db.LessonBookings.Add(new LessonBooking
            {
                InstructorProfileId = booking.InstructorProfileId,
                CarId = booking.CarId,
                StartsAt = booking.StartsAt,
                EndsAt = booking.EndsAt,
                Status = BookingStatus.Available
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, approve ? "Урокът е потвърден." : "Заявката е отказана, а часът отново е свободен.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(
        int bookingId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var booking = await db.LessonBookings.FirstOrDefaultAsync(item =>
            item.Id == bookingId && item.StudentProfileId == studentId, cancellationToken);
        if (booking is null || (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed))
        {
            return (false, "Този урок не може да бъде отменен.");
        }
        if (booking.StartsAt <= DateTime.Now.AddHours(24))
        {
            return (false, "Урокът може да бъде отменен до 24 часа преди началото.");
        }

        booking.Status = BookingStatus.Cancelled;
        db.LessonBookings.Add(new LessonBooking
        {
            InstructorProfileId = booking.InstructorProfileId,
            CarId = booking.CarId,
            StartsAt = booking.StartsAt,
            EndsAt = booking.EndsAt,
            Status = BookingStatus.Available
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Урокът е отменен и часът отново е свободен.");
    }

    public async Task<(bool Success, string Message)> CompleteAsync(
        int bookingId,
        int instructorId,
        string summary,
        int vehicleControl,
        int roadAwareness,
        int confidence,
        string comment,
        CancellationToken cancellationToken = default)
    {
        var booking = await db.LessonBookings
            .Include(item => item.Lesson)
            .FirstOrDefaultAsync(item => item.Id == bookingId && item.InstructorProfileId == instructorId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Confirmed || booking.Lesson is not null)
        {
            return (false, "Само потвърден урок може да бъде отбелязан като проведен.");
        }
        if (vehicleControl is < 1 or > 5 || roadAwareness is < 1 or > 5 || confidence is < 1 or > 5)
        {
            return (false, "Оценките трябва да бъдат между 1 и 5.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        booking.Status = BookingStatus.Completed;
        var lesson = new Lesson { Booking = booking, Summary = summary.Trim(), CompletedAt = DateTime.UtcNow };
        lesson.Evaluation = new Evaluation
        {
            Lesson = lesson,
            VehicleControl = vehicleControl,
            RoadAwareness = roadAwareness,
            Confidence = confidence,
            Comment = comment.Trim()
        };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(cancellationToken);

        var student = await db.StudentProfiles
            .Include(item => item.Bookings)
            .SingleAsync(item => item.Id == booking.StudentProfileId, cancellationToken);
        var total = student.Bookings.Count(item => item.Status == BookingStatus.Completed);
        student.ProgressPercent = Math.Min(100, total * 5);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Урокът и оценката са записани.");
    }
}
