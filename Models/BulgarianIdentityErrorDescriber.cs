using Microsoft.AspNetCore.Identity;

namespace DriveSchool.Models;

public sealed class BulgarianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error("Грешка", "Възникна неочаквана грешка.");
    public override IdentityError ConcurrencyFailure() => Error("Промяна", "Записът е променен. Презаредете страницата и опитайте отново.");
    public override IdentityError PasswordMismatch() => Error("Парола", "Невалидна парола.");
    public override IdentityError InvalidToken() => Error("Токен", "Невалиден или изтекъл код.");
    public override IdentityError DuplicateUserName(string userName) => Error("Потребител", "Този имейл вече се използва.");
    public override IdentityError InvalidUserName(string? userName) => Error("Потребител", "Въведете валиден имейл адрес.");
    public override IdentityError DuplicateEmail(string email) => Error("Имейл", "Този имейл вече е регистриран.");
    public override IdentityError InvalidEmail(string? email) => Error("Имейл", "Въведете валиден имейл адрес.");
    public override IdentityError PasswordTooShort(int length) => Error("Кратка парола", $"Паролата трябва да е поне {length} знака.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error("Символ", "Паролата трябва да съдържа поне един специален символ.");
    public override IdentityError PasswordRequiresDigit() => Error("Цифра", "Паролата трябва да съдържа поне една цифра.");
    public override IdentityError PasswordRequiresLower() => Error("Малка буква", "Паролата трябва да съдържа малка буква.");
    public override IdentityError PasswordRequiresUpper() => Error("Главна буква", "Паролата трябва да съдържа главна буква.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error("Разнообразие", $"Паролата трябва да съдържа поне {uniqueChars} различни знака.");
    public override IdentityError UserAlreadyHasPassword() => Error("Парола", "Профилът вече има парола.");
    public override IdentityError UserLockoutNotEnabled() => Error("Блокиране", "Блокирането не е разрешено за този профил.");

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };
}
