using FYPBidNetra.Models;
using FYPBidNetra.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;


namespace FYPBidNetra.Controllers
{
    public class AccountController : Controller
    {
        private readonly FypContext _context;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;


        public AccountController(FypContext context, DataSecurityProvider p, IDataProtectionProvider provider, IWebHostEnvironment env)
        {
            _context = context;
            _protector = provider.CreateProtector(p.Key);
            _env = env;
        }


        // Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(UserListEdit u)
        {
            //return Json(u);  
            try
            {
                // Validate email format
                if (!Regex.IsMatch(u.EmailAddress, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    ModelState.AddModelError("EmailAddress", "Please enter a valid email address");
                    return View(u);
                }

                // Validate phone number format (Nepal)
                if (!Regex.IsMatch(u.Phone, @"^\+977[0-9]{10}$"))
                {
                    ModelState.AddModelError("Phone", "Phone number must start with +977 followed by 10 digits");
                    return View(u);
                }

                var users = _context.UserLists.Where(x => x.EmailAddress == u.EmailAddress).FirstOrDefault();
                if (users == null)
                {
                    short maxid;
                    if (_context.UserLists.Any())
                        maxid = Convert.ToInt16(_context.UserLists.Max(x => x.UserId) + 1);
                    else
                        maxid = 1;
                    u.UserId = maxid;


                    if (u.UserFile != null)
                    {
                        string fileName = "UserImage" + Guid.NewGuid() + Path.GetExtension(u.UserFile.FileName);
                        string filePath = Path.Combine(_env.WebRootPath, "UserImage", fileName);
                        
                        if (!Directory.Exists(Path.Combine(_env.WebRootPath, "UserImage")))
                        {
                            Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "UserImage"));
                        }
                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            u.UserFile.CopyTo(stream);
                        }
                        u.UserPhoto = fileName;
                    }


                    UserList userList = new()
                    {
                        UserId = u.UserId,
                        FirstName = u.FirstName,
                        MiddleName = u.MiddleName,
                        LastName = u.LastName,
                        Phone = u.Phone,
                        EmailAddress = u.EmailAddress,
                        Province = u.Province,
                        District = u.District,
                        City = u.City,
                        Gender = u.Gender,
                        UserPhoto = u.UserPhoto,
                        UserPassword = _protector.Protect(u.UserPassword),
                        UserRole = u.UserRole,
                        //UserRole = "Admin"
                    };

                    //return Json(userList);
                    _context.Add(userList);
                    _context.SaveChanges();

                    if (u.UserRole == "Bidder")
                    {
                        short bidderid;
                        if (_context.Companies.Any())
                            bidderid = Convert.ToInt16(_context.Companies.Max(x => x.CompanyId) + 1);
                        else
                            bidderid = 1;
                        u.CompanyId = bidderid;

                        short bankid;
                        if (_context.Companies.Any())
                            bankid = Convert.ToInt16(_context.Banks.Max(x => x.BankId) + 1);
                        else
                            bankid = 1;
                        u.BankId = bankid;

                        if (u.RegisterFile != null)
                        {
                            string fileName = "RegisterImage" + Guid.NewGuid() + Path.GetExtension(u.RegisterFile.FileName);
                            string filePath = Path.Combine(_env.WebRootPath, "RegisterImage", fileName);
                            // Ensure the EmpImage directory exists
                            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "RegisterImage")))
                            {
                                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "RegisterImage"));
                            }
                            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                            {
                                u.RegisterFile.CopyTo(stream);
                            }
                            u.RegistrationDocument = fileName;
                        }

                        if (u.PanFile != null)
                        {
                            string fileName = "PanImage" + Guid.NewGuid() + Path.GetExtension(u.PanFile.FileName);
                            string filePath = Path.Combine(_env.WebRootPath, "PanImage", fileName);
                            // Ensure the EmpImage directory exists
                            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "PanImage")))
                            {
                                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "PanImage"));
                            }
                            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                            {
                                u.PanFile.CopyTo(stream);
                            }
                            u.PanDocument = fileName;
                        }




                        Company company = new()
                        {
                            CompanyId = u.CompanyId,
                            UserbidId = u.UserId,
                            CompanyName = u.CompanyName,
                            FullAddress = u.FullAddress,
                            OfficeEmail = u.OfficeEmail,
                            CompanyWebsiteUrl = u.CompanyWebsiteUrl,
                            CompanyType = u.CompanyType,
                            RegistrationDocument = u.RegistrationDocument,
                            PanDocument = u.PanDocument,
                            RegistrationNumber = u.RegistrationNumber,
                            PanNumber = u.PanNumber,
                            Rating = u.Rating,
                            Position = u.Position
                        };
                         //return Json(company);
                        Bank bank = new()
                        {
                            BankId = u.BankId,
                            BankName = u.BankName,
                            AccountNumber = u.AccountNumber,
                            AccountHolderName = u.AccountHolderName,
                            AccountType = u.AccountType,
                            UserbankId = u.UserId
                        };

                        //return Json(bank);
                        _context.Companies.Add(company);
                        _context.Banks.Add(bank);
                        _context.SaveChanges();

                    }
                    // Set success message in TempData
                    TempData["SuccessMessage"] = "Registration successful! You can now log in.";

                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    TempData["ErrorMessage"] = "User already exists with this email!";
                    return View(u);
                }
            }
            catch
            {
                ModelState.AddModelError("", "User Registration Failed. Please try again");
                return View(u);
            }
        }


        //login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserListEdit uEdit)
        {
            var users = _context.UserLists.ToList();
            if (users != null)
            {
                var u = users.Where(x => x.EmailAddress.ToUpper().Equals(uEdit.EmailAddress.ToUpper()) && _protector.Unprotect(x.UserPassword).Equals(uEdit.UserPassword)).FirstOrDefault();

                if (u != null)
                {
                    List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, u.UserId.ToString()),
                new Claim(ClaimTypes.Role, u.UserRole),

                new Claim("Role", u.UserRole),
                new Claim("FullName", u.FirstName),
                new Claim("image", u.UserPhoto),
                new Claim("email", u.EmailAddress),
                // new Claim("address",u.CurrentAddress),
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    return RedirectToAction("Dashboard");
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid User");

            }
            return View(uEdit);
        }




        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (User.IsInRole("Publisher"))
            {
                return RedirectToAction("Index", "PublisherTender");
            }
            else
            {
                return RedirectToAction("Index", "BidTender");
            }

        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(ChangePassword c)
        {
            var u = _context.UserLists.Where(e => e.UserId == Convert.ToInt16(User.Identity!.Name)).First();
            if (_protector.Unprotect(u.UserPassword) != c.CurrentPassword)
            {
                ModelState.AddModelError("", "Check your current password");
            }
            else
            {
                if (c.NewPassword == c.ConfirmPassword)
                {
                    u.UserPassword = _protector.Protect(c.NewPassword);
                    _context.Update(u);
                    _context.SaveChanges();

                    // Add a success message to TempData
                    TempData["Success"] = "Your password has been changed successfully!";
                    return View();
                }
                else
                {
                    ModelState.AddModelError("", "Confirm Password does not match");
                    return View(c);
                }
            }

            TempData["Error"] = "Password change failed. Please try again!";
            return View();
        }


        [HttpGet]

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(UserListEdit edit)
        {

            if (edit.EmailAddress != null)
            {
                Random r = new Random();
                HttpContext.Session.SetString("token", r.Next(9999).ToString());
                var token = HttpContext.Session.GetString("token");
                var user = _context.UserLists.Where(u => u.EmailAddress == edit.EmailAddress).FirstOrDefault();
                if (user != null)
                {
                    SmtpClient s = new()
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        Credentials = new NetworkCredential("ghastlybarely2356@gmail.com", "pfkz vkkg jrpg xlpn"),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    MailMessage m = new()
                    {
                        From = new MailAddress("ghastlybarely2356@gmail.com"),
                        Subject = "Forgot Password Token",
                        Body = $@"<p class='text-red-800' style='background-color:red;'>Forgot Password</p>
                        <a href='https://localhost:44321/Account/ResetPassword?UserId=0&EmailAddress={user.EmailAddress}&EmailToken={_protector.Protect(token)}' style='background-color:blue;' >ResetPassword</a>
:{token}",
                        IsBodyHtml = true,

                    };


                    m.To.Add(user.EmailAddress);
                    s.Send(m);
                    // return Json("success");
                    return RedirectToAction("VerifyToken", new { email = user.EmailAddress });

                }
                else
                {
                    ModelState.AddModelError("", "This Email is not registered Email.");
                    return View(edit);
                }
            }
            return Json("Failed");
        }

        [HttpGet]
        public IActionResult VerifyToken(string email)
        {
            return View(new UserListEdit { EmailAddress = email });
        }

        [HttpPost]
        public IActionResult VerifyToken(UserListEdit e)
        {
            var token = HttpContext.Session.GetString("token");
            if (token == e.EmailToken)
            {
                var et = _protector.Protect(e.EmailToken!);
                return RedirectToAction("ResetPassword", new UserListEdit { EmailAddress = e.EmailAddress, EmailToken = et });
            }
            else
            {
                return Json("Failed");
            }
        }


        [HttpGet]
        public IActionResult ResetPassword(UserListEdit e)
        {
            try
            {
                // return Json(e);
                var token = HttpContext.Session.GetString("token");
                var eToken = _protector.Unprotect(e.EmailToken);
                if (token == eToken)
                {
                    return View(new ChangePassword { EmailAddress = e.EmailAddress });
                }
                else
                {
                    return RedirectToAction("ForgotPassword");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("ForgotPassword");
            }
        }


        [HttpPost]
        public IActionResult ResetPassword(ChangePassword model)
        {


            if (model.NewPassword == model.ConfirmPassword)
            {
                var user = _context.UserLists.FirstOrDefault(u => u.EmailAddress == model.EmailAddress);
                if (user != null)
                {
                    user.UserPassword = _protector.Protect(model.NewPassword);
                    _context.Update(user);
                    _context.SaveChanges();
                    return RedirectToAction("Login");
                }
            }
            else
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View(model);
            }


            // return RedirectToAction("ForgotPassword");
            return Json("error");
        }
    }

}
