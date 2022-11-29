using HUK_Part_2_.Entities;
using HUK_Part_2_.Entities.Context;
using HUK_Part_2_.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NETCore.Encrypt.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HUK_Part_2_.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly DatabaseContext _dataBaseContext;
        private readonly IConfiguration _configuration;

        public AccountController(DatabaseContext dataBaseContext, IConfiguration configuration)
        {
            _dataBaseContext = dataBaseContext;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashedPassword = DoMD5HashedString(model.Password);

                User currentUser = _dataBaseContext.Users.SingleOrDefault(x => x.Username.ToLower() == model.Username.ToLower() && x.Password == hashedPassword);
                if (currentUser != null)
                {
                    if (currentUser.Locked)
                    {
                        ModelState.AddModelError(nameof(model.Username), "User is locked.");
                        return View(model);
                    }

                    List<Claim> claims = new();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, currentUser.Id.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, currentUser.FullName ?? string.Empty));
                    claims.Add(new Claim(ClaimTypes.Role, currentUser.Role));
                    claims.Add(new Claim("Username", currentUser.Username));

                    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    ClaimsPrincipal principal = new(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Username or password incorrect.");
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_dataBaseContext.Users.Any(x => x.Username.ToLower() == model.Username.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.Username), "Username already exists.");
                    return View(model);
                }

                string hashedPassword = DoMD5HashedString(model.Password);

                User user = new()
                {
                    Username = model.Username,
                    Password = hashedPassword
                };

                _dataBaseContext.Users.Add(user);
                int affectedRowCount = _dataBaseContext.SaveChanges();

                if (affectedRowCount == 0)
                {
                    ModelState.AddModelError("", "User con not be added.");
                }
                else
                {
                    return RedirectToAction(nameof(Login));
                }
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Profile()
        {
            ProfileInfoLoader();

            return View();
        }

        [HttpPost]
        public IActionResult ProfileChangeFullName([Required][StringLength(50)] string? fullname)
        {
            if (ModelState.IsValid)
            {
                var user = GetCurrentUser();

                user.FullName = fullname;
                _dataBaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }

            ProfileInfoLoader();
            return View("Profile");
        }

        [HttpPost]
        public IActionResult ProfileChangePassword([Required][MinLength(6)][MaxLength(18)] string? password)
        {
            if (ModelState.IsValid)
            {
                var user = GetCurrentUser();

                user.Password = DoMD5HashedString(password);

                _dataBaseContext.SaveChanges();

                ViewData["result"] = "PasswordChanged";
            }

            ProfileInfoLoader();
            return View("Profile");
        }

        [HttpPost]
        public IActionResult ProfileChangeProfileImage([Required] IFormFile file)
        {
            if (ModelState.IsValid)
            {
                var currentUser = GetCurrentUser();
                string fileName = $"p_{currentUser.Id}.jpg";

                Stream stream = new FileStream($"wwwroot/uploads/{fileName}",FileMode.OpenOrCreate);
                file.CopyTo(stream);
                stream.Close();
                stream.Dispose();

                currentUser.ProfileImageFileName = fileName;
                _dataBaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }

            ProfileInfoLoader();
            return View("Profile");
        }

        private User GetCurrentUser()
        {
            var currentUserId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return _dataBaseContext.Users.SingleOrDefault(x => x.Id == currentUserId);
        }

        private void ProfileInfoLoader()
        {
            Guid userId = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            User user = _dataBaseContext.Users.SingleOrDefault(x => x.Id == userId);

            ViewData["FullName"] = user.FullName;
        }

        private string DoMD5HashedString(string str)
        {
            string md5Salt = _configuration.GetValue<string>("AppSettings:MD5Salt");
            string salted = $"{str}{md5Salt}";
            return salted.MD5();
        }
    }
}
