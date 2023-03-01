using Micro_Social_Network.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Micro_Social_Network.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService <DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurileinca o data
                // Acesta metoda trebuie sa se execute o singura data
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }

                // CREAREA ROLURILOR IN BD
                // daca nu contine roluri, acestea se vor crea
                context.Roles.AddRange(
                new IdentityRole
                {
                    Id = "1098f30e-4876-4ebe-afd2-e75e9d9fc10e", Name = "Admin", NormalizedName = "Admin".ToUpper() },
                new IdentityRole
                {
                    Id = "650fd968-3e00-4168-9fec-b110d0e8dab3", Name = "User", NormalizedName = "User".ToUpper() }
                );

                // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
                // parolele sunt de tip hash
                var hasher = new PasswordHasher<ApplicationUser>();

                // CREAREA USERILOR IN BD
                // Se creeaza cate un user pentru fiecare rol
                context.Users.AddRange(
                new ApplicationUser
                {
                    Id = "d8025b90-4b6f-421e-8b47-719fbfeca83c",
                    // primary key
                    UserName = "admin@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedUserName = "ADMIN@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Admin1!"),
                },
                new ApplicationUser
                {
                    Id = "d8025b90-4b6f-421e-8b47-719fbfeca83d",
                    // primary key
                    UserName = "user@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User!"),
                 
                });

                // ASOCIEREA USER-ROLE
                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    RoleId = "1098f30e-4876-4ebe-afd2-e75e9d9fc10e",
                    UserId = "d8025b90-4b6f-421e-8b47-719fbfeca83c"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "650fd968-3e00-4168-9fec-b110d0e8dab3",
                    UserId = "d8025b90-4b6f-421e-8b47-719fbfeca83d"
                }
                );
                context.SaveChanges();
            }
        }
    }


}



