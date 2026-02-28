using Microsoft.AspNetCore.Identity;

namespace InventoryManagementPro.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            string[] roles = { "Admin", "Staff" };
            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }
            var adminEmail = "admin@ims.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            var staffEmail = "staff@ims.com";
            var staff = await userManager.FindByEmailAsync(staffEmail);
            if (staff == null)
            {
                staff = new IdentityUser { UserName = staffEmail, Email = staffEmail, EmailConfirmed = true };
                await userManager.CreateAsync(staff, "Staff123!");
                await userManager.AddToRoleAsync(staff, "Staff");
            }
        }
    }
}