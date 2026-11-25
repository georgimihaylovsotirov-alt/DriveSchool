
using DriveSchool.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Data;

public static class SeedData
{
    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSuccess(result);
            }
        }
    }

    public static async Task InitializeOwnerIfConfiguredAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var email = configuration["DRIVESCHOOL_OWNER_EMAIL"]?.Trim();
        var password = configuration["DRIVESCHOOL_OWNER_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "За първоначален собственик задайте и DRIVESCHOOL_OWNER_EMAIL, и DRIVESCHOOL_OWNER_PASSWORD.");
        }

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Собственик на DriveSchool"
            };

            var created = await users.CreateAsync(user, password);
            EnsureSuccess(created);
        }

        if (!await users.IsInRoleAsync(user, AppRoles.Owner))
        {
            EnsureSuccess(await users.AddToRoleAsync(user, AppRoles.Owner));
        }
    }

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await EnsureRolesAsync(services);

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<DriveSchoolDbContext>();

        var instructors = new List<InstructorProfile>();

        var instructorNames = new[]
        {
            ("georgi@driveschool.bg", "Георги Димитров",
                "Спокоен подход и 12 години опит в градско шофиране."),

            ("elena@driveschool.bg", "Елена Петрова",
                "Подготвя курсисти за уверено и безопасно шофиране."),

            ("nikolay@driveschool.bg", "Николай Иванов",
                "Специализира в практическото обучение за категория B.")
        };

        for (var i = 0; i < instructorNames.Length; i++)
        {
            var (email, name, bio) = instructorNames[i];

            var user = await EnsureUserAsync(
                users, email, name, AppRoles.Instructor);

            var profile = await db.InstructorProfiles
                .FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id);

            if (profile is null)
            {
                profile = new InstructorProfile
                {
                    ApplicationUserId = user.Id,
                    Bio = bio,
                    YearsExperience = 7 + i * 3,
                    Categories = "B"
                };

                db.InstructorProfiles.Add(profile);
                await db.SaveChangesAsync();
            }

            instructors.Add(profile);
        }

        // Примерни автомобили
        if (!await db.Cars.AnyAsync())
        {
            db.Cars.AddRange(
                new Car
                {
                    RegistrationNumber = "CB 4101 AB",
                    MakeModel = "Toyota Corolla",
                    Category = "B",
                    Year = 2022
                },
                new Car
                {
                    RegistrationNumber = "CB 5202 BC",
                    MakeModel = "Škoda Fabia",
                    Category = "B",
                    Year = 2021
                },
                new Car
                {
                    RegistrationNumber = "CB 6303 CD",
                    MakeModel = "Hyundai i20",
                    Category = "B",
                    Year = 2023
                },
                new Car
                {
                    RegistrationNumber = "CB 7404 DE",
                    MakeModel = "Dacia Sandero",
                    Category = "B",
                    Year = 2020
                },
                new Car
                {
                    RegistrationNumber = "CB 8505 EF",
                    MakeModel = "Volkswagen Golf",
                    Category = "B",
                    Year = 2022
                });

            await db.SaveChangesAsync();
        }

        // Примерен теоретичен тест
        if (!await db.TheoryTests.AnyAsync())
        {
            var test = new TheoryTest
            {
                Title = "Основи на безопасното движение",
                Category = TestCategory.B,
                TimeLimitMinutes = 20,
                PassingScore = 80,
                IsPublished = true,

                Questions =
                [
                    new Question
                    {
                        Text = "Какво трябва да направите при приближаване към пешеходна пътека?",
                        Answers =
                        [
                            new Answer
                            {
                                Text = "Да намалите скоростта и да пропуснете пресичащите пешеходци.",
                                IsCorrect = true
                            },
                            new Answer
                            {
                                Text = "Да подадете звуков сигнал и да продължите.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "Да ускорите, ако няма светофар.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "Да спрете само ако пешеходецът е стъпил на платното.",
                                IsCorrect = false
                            }
                        ]
                    },
                    new Question
                    {
                        Text = "Кога е задължително използването на предпазен колан?",
                        Answers =
                        [
                            new Answer
                            {
                                Text = "Само при движение извън населено място.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "При всяко пътуване, когато автомобилът е оборудван с колани.",
                                IsCorrect = true
                            },
                            new Answer
                            {
                                Text = "Само при скорост над 50 km/h.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "Само за водача.",
                                IsCorrect = false
                            }
                        ]
                    },
                    new Question
                    {
                        Text = "Как постъпвате при жълт сигнал на светофара?",
                        Answers =
                        [
                            new Answer
                            {
                                Text = "Ускорявате, за да преминете преди червения сигнал.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "Спирате, освен ако това би създало опасност.",
                                IsCorrect = true
                            },
                            new Answer
                            {
                                Text = "Продължавате без да променяте скоростта.",
                                IsCorrect = false
                            },
                            new Answer
                            {
                                Text = "Използвате аварийните светлини.",
                                IsCorrect = false
                            }
                        ]
                    }
                ]
            };

            db.TheoryTests.Add(test);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> users,
        string email,
        string fullName,
        string role)
    {
        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            EnsureSuccess(await users.CreateAsync(
                user, "DriveSchool.Dev2026!"));
        }
        else if (user.FullName != fullName)
        {
            user.FullName = fullName;
            EnsureSuccess(await users.UpdateAsync(user));
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            EnsureSuccess(await users.AddToRoleAsync(user, role));
        }

        return user;
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(error => error.Description)));
        }
    }
}
