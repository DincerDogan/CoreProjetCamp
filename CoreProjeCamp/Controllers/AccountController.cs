﻿using Business.ValidationRules.FluentValidation;
using Core.Utilities.Helpers;
using Entity.Identity;
using Entity.ViewModel;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CoreProjetCamp.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUser> _userManager;
        readonly SignInManager<AppUser> _signInManager;
        private readonly IHostingEnvironment _environment;
        RegisterValidator accountValidator = new RegisterValidator();

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IHostingEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Login(string ReturnUrl)
        {
            TempData["returnUrl"] = ReturnUrl;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            LogınValidator loginValidator = new LogınValidator();
            ValidationResult validationResult = loginValidator.Validate(loginViewModel);

            if (validationResult.IsValid)

            {
                AppUser user = await _userManager.FindByEmailAsync(loginViewModel.Email);
                if (user != null)
                {
                    //İlgili kullanıcıya dair önceden oluşturulmuş bir Cookie varsa siliyoruz.
                    await _signInManager.SignOutAsync();
                    Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, loginViewModel.Persistent, loginViewModel.Lock);
                    ;
                    if (result.Succeeded)
                    {
                        if (string.IsNullOrEmpty(TempData["returnUrl"] != null ? TempData["returnUrl"].ToString() : ""))
                            HttpContext.Session.SetString("Email", loginViewModel.Email);
                        HttpContext.Session.SetInt32("Id", loginViewModel.Id);

                        return RedirectToAction("Index", "Category");
                        return Redirect(TempData["returnUrl"].ToString());
                    }
                    else
                        await _userManager.AccessFailedAsync(user); //Eğer ki başarısız bir account girişi söz konusu ise AccessFailedCount kolonundaki değer +1 arttırılacaktır. 
                    int failcount = await _userManager.GetAccessFailedCountAsync(user); //Kullanıcının yapmış olduğu başarısız giriş deneme adedini alıyoruz.
                    if (failcount == 3)
                    {
                        await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(DateTime.Now.AddMinutes(1))); //Eğer ki başarısız giriş denemesi 3'ü bulduysa ilgili kullanıcının hesabını kilitliyoruz.
                        ModelState.AddModelError("Locked", "Art arda 3 başarısız giriş denemesi yaptığınızdan dolayı hesabınız 1 dk kitlenmiştir.");
                    }
                    else
                    {
                        if (result.IsLockedOut)
                            ModelState.AddModelError("Locked", "Art arda 3 başarısız giriş denemesi yaptığınızdan dolayı hesabınız 1 dk kilitlenmiştir.");
                        else
                            ModelState.AddModelError("NotUser2", "E-posta veya şifre yanlış.");
                    }

                }
                else
                {
                    ModelState.AddModelError("NotUser2", "Şifreniz Yanlış.");

                }
            }
            else
            {
                foreach (var item in validationResult.Errors)
                {
                    ModelState.AddModelError(item.PropertyName, item.ErrorMessage);
                }
            }
            return View();
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel register)
        {
            ValidationResult validationResult = accountValidator.Validate(register);
            if (validationResult.IsValid)
            {
                AppUser user = new AppUser();
                {
                    user.Name = register.Name.ToLower();
                    user.Email = register.Email.ToLower();
                    user.PhoneNumber = register.PhoneNumber;
                    user.SurName = register.SurName.ToLower();
                    user.UserName = register.UserName;
                    user.ImagePath = "DefaultPng";
                    user.PhoneNumberConfirmed = true;
                    await _userManager.AddToRoleAsync(user, "Misafir");
                    var result = await _userManager.CreateAsync(user, register.Password);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Logın", "Account");
                    }
                    else
                        foreach (var item in validationResult.Errors)
                        {
                            ModelState.AddModelError(item.PropertyName, item.ErrorMessage);
                        }
                }
            }
            return View();
        }

        public async Task<IActionResult> GetByList(AppUser model)
        {
            
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (model != null)
            {
                ViewBag.ImagePath = user.ImagePath.ToString();

            }
            return View(user);
        }
        [HttpGet]
        public IActionResult EditProfile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditUserViewModel model, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                HttpContext.Session.GetString("Email");
                AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
                //Resim Yükleme İşlemi
                if (model.ImagePath != null)
                {
                    ImageUpdate(model, user);
                }
                else
                    user.Name = model.Name;
                user.SurName = model.SurName;
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                IdentityResult result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    result.Errors.ToList().ForEach(e => ModelState.AddModelError(e.Code, e.Description));
                    return View(model);
                }
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.SignOutAsync();
                await _signInManager.SignInAsync(user, true);
            }
            return RedirectToAction("GetByList", "Account");
        }

        private static void ImageUpdate(EditUserViewModel model, AppUser user)
        {
            var extension = Path.GetExtension(model.ImagePath.FileName);//Resmin uzantısı
            var newImageName = Guid.NewGuid() + extension;
            var locatin = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/", newImageName);
            var stream = new FileStream(locatin, FileMode.Create);
            model.ImagePath.CopyTo(stream);
            user.ImagePath = newImageName;
        }

        [HttpPost]
        public async Task<IActionResult> EditPassword(EditPasswordViewModel model, IFormFile Getfile)
        {
            if (ModelState.IsValid)

            {

                AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

                if (await _userManager.CheckPasswordAsync(user, model.OldPassword))
                {
                    IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (!result.Succeeded)
                    {
                        result.Errors.ToList().ForEach(e => ModelState.AddModelError(e.Code, e.Description));
                        return View(model);
                    }
                    await _userManager.UpdateSecurityStampAsync(user);
                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(user, true);
                }
            }
            return RedirectToAction("Index");
        }
        public IActionResult PasswordReset()
        {
            return View();
        }
        public async Task<IActionResult> RoleAssign(string id)
        {
            AppUser user = await _userManager.FindByIdAsync(id);

            await _userManager.AddToRoleAsync(user, "Administrator");
            await _userManager.AddToRoleAsync(user, "Moderator");
            await _userManager.AddToRoleAsync(user, "Editor");
            await _userManager.AddToRoleAsync(user, "tor");
            return View();
        }
        public async Task<IActionResult> Deneme()
        {
            var result = _userManager.GetUsersInRoleAsync("Admin");
            return View(result);
            
        }



    }
}
