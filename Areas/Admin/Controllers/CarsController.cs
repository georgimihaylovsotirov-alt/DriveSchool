using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriveSchool.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Owner)]
public sealed class CarsController(DriveSchoolDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Cars.OrderBy(item => item.RegistrationNumber).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new CarEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var registration = model.RegistrationNumber.Trim().ToUpperInvariant();
        if (await db.Cars.AnyAsync(item => item.RegistrationNumber == registration))
        {
            ModelState.AddModelError(nameof(model.RegistrationNumber), "Този регистрационен номер вече е добавен.");
            return View(model);
        }
        db.Cars.Add(new Car
        {
            RegistrationNumber = registration,
            MakeModel = model.MakeModel.Trim(),
            Category = model.Category.Trim().ToUpperInvariant(),
            Year = model.Year,
            IsAvailable = model.IsAvailable
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Автомобилът е добавен.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var car = await db.Cars.FindAsync(id);
        if (car is null) return NotFound();
        return View(new CarEditViewModel
        {
            Id = car.Id,
            RegistrationNumber = car.RegistrationNumber,
            MakeModel = car.MakeModel,
            Category = car.Category,
            Year = car.Year,
            IsAvailable = car.IsAvailable
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CarEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var car = await db.Cars.FindAsync(model.Id);
        if (car is null) return NotFound();
        var registration = model.RegistrationNumber.Trim().ToUpperInvariant();
        if (await db.Cars.AnyAsync(item => item.Id != model.Id && item.RegistrationNumber == registration))
        {
            ModelState.AddModelError(nameof(model.RegistrationNumber), "Този регистрационен номер вече е добавен.");
            return View(model);
        }
        car.RegistrationNumber = registration;
        car.MakeModel = model.MakeModel.Trim();
        car.Category = model.Category.Trim().ToUpperInvariant();
        car.Year = model.Year;
        car.IsAvailable = model.IsAvailable;
        await db.SaveChangesAsync();
        TempData["Success"] = "Данните за автомобила са обновени.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var car = await db.Cars.FindAsync(id);
        return car is null ? NotFound() : View(car);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var car = await db.Cars.FindAsync(id);
        if (car is null) return NotFound();
        db.Cars.Remove(car);
        await db.SaveChangesAsync();
        TempData["Success"] = "Автомобилът е премахнат от автопарка.";
        return RedirectToAction(nameof(Index));
    }
}
