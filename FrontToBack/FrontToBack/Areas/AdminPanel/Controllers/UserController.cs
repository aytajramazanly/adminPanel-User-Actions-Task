using FrontToBack.Areas.AdminPanel.ViewModels;
using FrontToBack.Data;
using FrontToBack.DataAccessLayer;
using FrontToBack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FrontToBack.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class UserController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(AppDbContext dbContext, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1)
                return NotFound();
            if (((page - 1) * 10) >= await _dbContext.Users.CountAsync())
                page--;
            var totalPageCount = Math.Ceiling((decimal)await _dbContext.Users.CountAsync() / 10);
            if (page > totalPageCount)
                return NotFound();

            ViewBag.totalPageCount = totalPageCount;
            ViewBag.currentPage = page;
            var users = await _dbContext.Users.Where(x => x.Fullname!="Admin").Skip((page - 1) * 10).Take(10).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ChangeActivation(string username)
        {
            if (username == null)
                return BadRequest();
            var user = await _userManager.FindByNameAsync(username);
            if (user==null)
                return NotFound();
            var currentActivation = user.IsActive;
            switch (currentActivation)
            {
                case true:
                    user.IsActive = false;
                    break;
                case false:
                    user.IsActive = true;
                    break;
            }
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ChangeRole(string username)
        {
            if (username == null)
                return BadRequest();
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();


            string currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            if (currentRole == null)
                return NotFound();
            var roles=await _roleManager.Roles.ToListAsync();
            ViewBag.CurrentRole = currentRole;

            return View(roles); 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string username,string role)
        {
            if (username == null && role==null)
                return BadRequest();
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound();

            var existRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            
            
            if (existRole == null)
                return BadRequest();
         
            if (existRole == role)
            {
                ModelState.AddModelError("", "This is current Role");
                return View();
            }
            var removeResult = await _userManager.RemoveFromRoleAsync(user, existRole);
            if (!removeResult.Succeeded)
            {
                foreach (var item in removeResult.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
                return View();
            }
           
            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                foreach (var item in removeResult.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
                return View();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
