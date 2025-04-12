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
using System.Text.Json;
using System.Text;


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

        /* [AllowAnonymous]
         [HttpPost]
         public IActionResult Register(UserListEdit u)
         {
             //return Json(u);  
             try
             {
                 if (!ModelState.IsValid)
                 {
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
         }*/



       /* [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(UserListEdit u)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(u);

                

                var users = _context.UserLists.Where(x => x.EmailAddress == u.EmailAddress).FirstOrDefault();
                if (users != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this email!";
                    return View(u);
                }

                // Store files in TempData
                if (u.UserFile != null)
                {
                    using var ms = new MemoryStream();
                    await u.UserFile.CopyToAsync(ms);
                    TempData["UserFile"] = Convert.ToBase64String(ms.ToArray());
                    TempData["UserFileName"] = u.UserFile.FileName;
                }

                if (u.RegisterFile != null)
                {
                    using var ms = new MemoryStream();
                    await u.RegisterFile.CopyToAsync(ms);
                    TempData["RegisterFile"] = Convert.ToBase64String(ms.ToArray());
                    TempData["RegisterFileName"] = u.RegisterFile.FileName;
                }

                if (u.PanFile != null)
                {
                    using var ms = new MemoryStream();
                    await u.PanFile.CopyToAsync(ms);
                    TempData["PanFile"] = Convert.ToBase64String(ms.ToArray());
                    TempData["PanFileName"] = u.PanFile.FileName;
                }

                // Create DTO
                var userDTO = new UserRegistrationDTO
                {
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    LastName = u.LastName,
                    Phone = u.Phone,
                    EmailAddress = u.EmailAddress,
                    Province = u.Province,
                    District = u.District,
                    City = u.City,
                    Gender = u.Gender,
                    UserPassword = u.UserPassword,
                    UserRole = u.UserRole,
                    CompanyName = u.CompanyName,
                    FullAddress = u.FullAddress,
                    OfficeEmail = u.OfficeEmail,
                    CompanyWebsiteUrl = u.CompanyWebsiteUrl,
                    CompanyType = u.CompanyType,
                    RegistrationNumber = u.RegistrationNumber,
                    PanNumber = u.PanNumber,
                    Rating = u.Rating,
                    Position = u.Position,
                    BankName = u.BankName,
                    AccountNumber = u.AccountNumber,
                    AccountHolderName = u.AccountHolderName,
                    AccountType = u.AccountType
                };

                // Generate and store OTP
                Random r = new Random();
                string otp = r.Next(100000, 999999).ToString();
                HttpContext.Session.SetString("registerOTP", otp);
                HttpContext.Session.SetString("tempUserData", System.Text.Json.JsonSerializer.Serialize(userDTO));

                // Send OTP via email
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
                    Subject = "Email Verification for Registration",
                    Body = $@"<h2>Email Verification</h2>
                     <p>Your verification code is: <strong>{otp}</strong></p>
                     <p>Please use this code to complete your registration.</p>",
                    IsBodyHtml = true,
                };

                m.To.Add(u.EmailAddress);
                s.Send(m);

                return Json("otp sended successfully");

                //return RedirectToAction("VerifyRegistration", new { email = u.EmailAddress });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Registration Failed. Please try again");
                return View(u);
            }
        }*/


        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(UserListEdit u)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(u);

                var existingUser = _context.UserLists.FirstOrDefault(x => x.EmailAddress == u.EmailAddress);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this email!";
                    return View(u);
                }

                // Save files to temp location
                if (u.UserFile != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"user_{Guid.NewGuid()}");
                    using (var stream = System.IO.File.Create(tempPath))
                    {
                        await u.UserFile.CopyToAsync(stream);
                    }
                    HttpContext.Session.SetString("UserTempPath", tempPath);
                    HttpContext.Session.SetString("UserFileName", u.UserFile.FileName);
                }

                // Store user data in session
                var userData = new
                {
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Phone,
                    u.EmailAddress,
                    u.Province,
                    u.District,
                    u.City,
                    u.Gender,
                    u.UserPassword,
                    u.UserRole,
                };

                //return Json(userData);

                HttpContext.Session.SetString("UserData", JsonSerializer.Serialize(userData));

                // Generate and send OTP
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("RegisterOTP", otp);

                // Send OTP via email
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
                    Subject = "Email Verification for Registration",
                    Body = $@"<h2>Email Verification</h2>
                     <p>Your verification code is: <strong>{otp}</strong></p>
                     <p>Please use this code to complete your registration.</p>",
                    IsBodyHtml = true,
                };

                m.To.Add(u.EmailAddress);
                s.Send(m);

                return RedirectToAction("VerifyRegistration", new { email = u.EmailAddress });
            }
            catch (Exception ex)
            {
                CleanTempFiles();
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(u);
            }
        }



        [HttpGet]
        public IActionResult VerifyRegistration(string email)
        {
            return View(new UserListEdit { EmailAddress = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyRegistration(UserListEdit model)
        {
            try
            {
                var storedOTP = HttpContext.Session.GetString("RegisterOTP");
                var userDataJson = HttpContext.Session.GetString("UserData");

                if (storedOTP != model.EmailToken || string.IsNullOrEmpty(userDataJson))
                {
                    ModelState.AddModelError("", "Invalid verification code");
                    return View(model);
                }

                var userData = JsonSerializer.Deserialize<UserListEdit>(userDataJson);

                // Generate user ID
                short userId = _context.UserLists.Any()
                    ? (short)(_context.UserLists.Max(x => x.UserId) + 1)
                    : (short)1;

                // Handle user photo
                string userPhotoPath = null;
                if (HttpContext.Session.TryGetValue("UserTempPath", out var userTempPath))
                {
                    var tempPath = Encoding.UTF8.GetString(userTempPath);
                    var fileName = $"user_{userId}{Path.GetExtension(HttpContext.Session.GetString("UserFileName"))}";
                    userPhotoPath = await SaveFileToPermanentLocation(tempPath, "UserImage", fileName);
                }

                // Create and save user
                var user = new UserList
                {
                    UserId = userId,
                    FirstName = userData.FirstName,
                    MiddleName = userData.MiddleName,
                    LastName = userData.LastName,
                    Phone = userData.Phone,
                    EmailAddress = userData.EmailAddress,
                    Province = userData.Province,
                    District = userData.District,
                    City = userData.City,
                    Gender = userData.Gender,
                    UserPhoto = userPhotoPath,
                    UserPassword = _protector.Protect(userData.UserPassword),
                    UserRole = userData.UserRole
                };

                //return Json(user);
                _context.UserLists.Add(user);
                await _context.SaveChangesAsync();

                /*// Handle bidder registration
                if (userData.UserRole == "Bidder")
                {
                    // Handle registration document
                    string registrationDocPath = null;
                    if (HttpContext.Session.TryGetValue("RegisterTempPath", out var regTempPath))
                    {
                        var tempPath = Encoding.UTF8.GetString(regTempPath);
                        var fileName = $"reg_{userId}{Path.GetExtension(HttpContext.Session.GetString("RegisterFileName"))}";
                        registrationDocPath = await SaveFileToPermanentLocation(tempPath, "RegisterImage", fileName);
                    }

                    // Handle PAN document
                    string panDocPath = null;
                    if (HttpContext.Session.TryGetValue("PanTempPath", out var panTempPath))
                    {
                        var tempPath = Encoding.UTF8.GetString(panTempPath);
                        var fileName = $"pan_{userId}{Path.GetExtension(HttpContext.Session.GetString("PanFileName"))}";
                        panDocPath = await SaveFileToPermanentLocation(tempPath, "PanImage", fileName);
                    }

                    // Create company
                    short companyId = _context.Companies.Any()
                        ? (short)(_context.Companies.Max(x => x.CompanyId) + 1)
                        : (short)1;

                    var company = new Company
                    {
                        CompanyId = companyId,
                        UserbidId = userId,
                        CompanyName = userData.CompanyName,
                        FullAddress = userData.FullAddress,
                        OfficeEmail = userData.OfficeEmail,
                        CompanyWebsiteUrl = userData.CompanyWebsiteUrl,
                        CompanyType = userData.CompanyType,
                        RegistrationDocument = registrationDocPath,
                        PanDocument = panDocPath,
                        RegistrationNumber = userData.RegistrationNumber,
                        PanNumber = userData.PanNumber,
                        Rating = userData.Rating,
                        Position = userData.Position
                    };

                    //return Json(company);
                    // Create bank
                    short bankId = _context.Banks.Any()
                        ? (short)(_context.Banks.Max(x => x.BankId) + 1)
                        : (short)1;

                    var bank = new Bank
                    {
                        BankId = bankId,
                        BankName = userData.BankName,
                        AccountNumber = userData.AccountNumber,
                        AccountHolderName = userData.AccountHolderName,
                        AccountType = userData.AccountType,
                        UserbankId = userId
                    };
                    //return Json(bank);
                    _context.Companies.Add(company);
                    _context.Banks.Add(bank);
                }

                await _context.SaveChangesAsync();*/
                CleanTempFiles();
                ClearSessionData();

                TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                CleanTempFiles();
                ModelState.AddModelError("", "Registration verification failed. Please try again.");
                return View(model);
            }
        }


        private async Task<string> SaveFileToPermanentLocation(string tempPath, string folder, string fileName)
        {
            var permPath = Path.Combine(_env.WebRootPath, folder, fileName);

            if (!Directory.Exists(Path.Combine(_env.WebRootPath, folder)))
            {
                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, folder));
            }

            using (var sourceStream = System.IO.File.OpenRead(tempPath))
            using (var destinationStream = System.IO.File.Create(permPath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }

            System.IO.File.Delete(tempPath);
            return fileName;
        }

        private void CleanTempFiles()
        {
            var keys = new[] { "UserTempPath", "RegisterTempPath", "PanTempPath" };
            foreach (var key in keys)
            {
                if (HttpContext.Session.TryGetValue(key, out var pathBytes))
                {
                    var path = Encoding.UTF8.GetString(pathBytes);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
            }
        }

        private void ClearSessionData()
        {
            var keys = new[] { "RegisterOTP", "UserData", "UserFileName", "RegisterFileName", "PanFileName" };
            foreach (var key in keys)
            {
                HttpContext.Session.Remove(key);
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
