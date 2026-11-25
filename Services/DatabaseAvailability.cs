namespace DriveSchool.Services;

public sealed class DatabaseAvailability(bool isConfigured)
{
    public bool IsConfigured { get; } = isConfigured;
}
