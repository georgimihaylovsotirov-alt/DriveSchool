using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public sealed class TheoryTestsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.TheoryTests
            .Include(test => test.Questions)
            .OrderBy(test => test.Title)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new TheoryTestCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TheoryTestCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        db.TheoryTests.Add(new TheoryTest
        {
            Title = model.Title.Trim(),
            Category = model.Category,
            TimeLimitMinutes = model.TimeLimitMinutes,
            PassingScore = model.PassingScore,
            IsPublished = false
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Тестът е създаден.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AddQuestion(int testId)
    {
        if (!await db.TheoryTests.AnyAsync(item => item.Id == testId)) return NotFound();
        ViewBag.TestTitle = await db.TheoryTests.Where(item => item.Id == testId).Select(item => item.Title).SingleAsync();
        return View(new TheoryQuestionCreateViewModel { TheoryTestId = testId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(TheoryQuestionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TestTitle = await db.TheoryTests.Where(item => item.Id == model.TheoryTestId).Select(item => item.Title).FirstOrDefaultAsync();
            return View(model);
        }
        if (!await db.TheoryTests.AnyAsync(item => item.Id == model.TheoryTestId)) return NotFound();
        db.Questions.Add(new Question
        {
            TheoryTestId = model.TheoryTestId,
            Text = model.Text.Trim(),
            Answers =
            [
                new Answer { Text = model.Answer1.Trim(), IsCorrect = model.CorrectAnswer == 1 },
                new Answer { Text = model.Answer2.Trim(), IsCorrect = model.CorrectAnswer == 2 },
                new Answer { Text = model.Answer3.Trim(), IsCorrect = model.CorrectAnswer == 3 },
                new Answer { Text = model.Answer4.Trim(), IsCorrect = model.CorrectAnswer == 4 }
            ]
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Въпросът е добавен към теста.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var test = await db.TheoryTests.FindAsync(id);
        if (test is null) return NotFound();
        return View(new TheoryTestCreateViewModel
        {
            Id = test.Id,
            Title = test.Title,
            Category = test.Category,
            TimeLimitMinutes = test.TimeLimitMinutes,
            PassingScore = test.PassingScore,
            IsPublished = test.IsPublished
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TheoryTestCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var test = await db.TheoryTests.FindAsync(model.Id);
        if (test is null) return NotFound();
        if (model.IsPublished && !await db.Questions.AnyAsync(question => question.TheoryTestId == test.Id))
        {
            ModelState.AddModelError(string.Empty, "Преди публикуване добавете поне един въпрос.");
            return View(model);
        }
        test.Title = model.Title.Trim();
        test.Category = model.Category;
        test.TimeLimitMinutes = model.TimeLimitMinutes;
        test.PassingScore = model.PassingScore;
        test.IsPublished = model.IsPublished;
        await db.SaveChangesAsync();
        TempData["Success"] = "Настройките на теста са запазени.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublished(int id)
    {
        var test = await db.TheoryTests.FindAsync(id);
        if (test is null) return NotFound();
        if (!test.IsPublished && !await db.Questions.AnyAsync(question => question.TheoryTestId == test.Id))
        {
            TempData["Error"] = "Преди публикуване добавете поне един въпрос.";
            return RedirectToAction(nameof(Index));
        }
        test.IsPublished = !test.IsPublished;
        await db.SaveChangesAsync();
        TempData["Success"] = test.IsPublished ? "Тестът е публикуван." : "Тестът е скрит от курсистите.";
        return RedirectToAction(nameof(Index));
    }
}
