using Book_Store.Data;
using BookStore.Models;
using BookStore.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.DbInitilizer
{
	public class DbInitilizer : IDbInitilizer
	{

		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly ApplicationDbContext _db;

        public DbInitilizer(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,ApplicationDbContext db )
        {
            _roleManager = roleManager;
			_userManager = userManager;
			_db = db;
        }
        public void Initilalize()
		{
			try
			{
				if (_db.Database.GetAppliedMigrations().Count() > 0)
				{
					_db.Database.Migrate();
				}

			}
			catch(Exception ex) { }


			if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
			{
				_roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
				_roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
				_roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
				_roleManager.CreateAsync(new IdentityRole(SD.Company)).GetAwaiter().GetResult();


				_userManager.CreateAsync(new ApplicationUser
				{
					UserName = "mohameds3add@gmail.com",
					Email = "mohameds3add@gmail.com",
					Name = "Mohamed Saad",
					PhoneNumber = "111111111",
					StreetAddress = "test 123 Ave",
					State = "IL",
					PostalCode = "23422",
					City = "cairo"
				}, "Admin123#").GetAwaiter().GetResult();

				ApplicationUser user = _db.applicationUsers.FirstOrDefault(u => u.Email == "mohameds3add@gmail.com");
				_userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();

			}

			return;
		}
	}
}
