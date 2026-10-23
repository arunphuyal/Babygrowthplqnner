using Microsoft.EntityFrameworkCore;
using PottyTrain.Api.Domain;
using PottyTrain.Api.Services;

namespace PottyTrain.Api.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDb db, PasswordService pw, IConfiguration cfg, ILogger logger)
    {
        await db.Database.MigrateAsync();

        var adminEmail = cfg["Seed:AdminEmail"] ?? "admin@pottytrain.local";
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var password = cfg["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                password = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
                logger.LogWarning("Seeded admin {Email} with generated password: {Password} (set Seed:AdminPassword to choose your own)", adminEmail, password);
            }
            db.Users.Add(new User { Email = adminEmail.ToLowerInvariant(), DisplayName = "Admin", Role = UserRole.Admin, Plan = Plan.Family, PasswordHash = pw.Hash(password) });
        }

        if (!await db.PlanOffers.AnyAsync())
        {
            db.PlanOffers.AddRange(
                new PlanOffer { Plan = Plan.Free, Name = "Free", MonthlyPrice = 0, YearlyPrice = 0, Features = "1 child\nQuick logging of pee, poop & accidents\n7-day history" },
                new PlanOffer { Plan = Plan.Premium, Name = "Premium", MonthlyPrice = 4.99m, YearlyPrice = 39.99m, Features = "Smart potty-time predictions\nFull history & insights\nReward charts\nExpert guides" },
                new PlanOffer { Plan = Plan.Family, Name = "Family", MonthlyPrice = 7.99m, YearlyPrice = 59.99m, Features = "Everything in Premium\nUnlimited children\nShare with caregivers\nDaycare report export" });
        }

        if (!await db.Articles.AnyAsync())
        {
            db.Articles.AddRange(Guides.All);
        }
        await db.SaveChangesAsync();
    }
}
