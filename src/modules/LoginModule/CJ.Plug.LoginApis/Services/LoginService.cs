using CJ.Plug.LoginApis.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CJ.Plug.LoginApis.Services
{
    public class LoginService : ILoginService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<UserRole> _roleManager;
        private readonly SignInManager<User> _signInManager;
        //private readonly IEmailService _emailService;
        //private readonly IConfiguration _config;

        public LoginService(
            UserManager<User> userManager,
            RoleManager<UserRole> roleManager,
            SignInManager<User> signInManager)
        //IEmailService emailService,)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            //_emailService = emailService;
            //_config = config;
        }

        // 注册用户
        public async Task<User?> RegisterUserAsync(User dto)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(dto.UserName);
                if (user != null)
                {
                    await _userManager.ChangePasswordAsync(user, user.Password, dto.Password);
                    return user;
                }
                var result = await _userManager.CreateAsync(dto, dto.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                    return null;
                }
                //Console.WriteLine("注册成功");
                await _roleManager.CreateAsync(new UserRole { Name = "TestRole" });
                return dto;
                // 发送验证邮件
                //await _emailService.SendVerificationEmailAsync(user.Email, await _userManager.GenerateEmailVerificationTokenAsync(user));
            }
            catch (Exception ex)
            {
                Console.WriteLine("注册失败: " + ex.Message);
                return null;
            }


        }

        // 用户登录
        public async Task<User?> LoginUserAsync(User dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
            {
                return null;
            }
            // 检查账号是否已被锁定
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                Console.WriteLine("账号已被锁定: " + user.UserName);
                return null;
            }
            Console.WriteLine("user: " + user?.UserName);
            Console.WriteLine("password: " + user?.Password);
            Console.WriteLine("user: " + dto?.UserName);
            Console.WriteLine("password: " + dto?.Password);
            var result1 = await _userManager.CheckPasswordAsync(user, dto.Password);
            Console.WriteLine("result1: " + result1);
            if (!result1)
            {
                return null;
            }

            // 生成JWT令牌
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                //new Claim(ClaimTypes.Email, user.Email)
                //new Claim(ClaimTypes.Role, user.Roles.First()) // 假设用户有角色
            };

            var identity = new ClaimsIdentity(claims, "JWT");
            var principal = new ClaimsPrincipal(identity);
            await _signInManager.SignInAsync(user, isPersistent: true);

            return user;
        }

        // 生成JWT令牌
        //private string GenerateJwtToken(User user)
        //{
        //    var securityKey = _config["Jwt:SecurityKey"];
        //    var issuer = _config["Jwt:Issuer"];
        //    var audience = _config["Jwt:Audience"];

        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(securityKey);
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(claims: GetClaims(user)),
        //        Expires = DateTime.UtcNow.AddDays(7),
        //        Issuer = issuer,
        //        Audience = audience,
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        //    };

        //    return tokenHandler.CreateToken(tokenDescriptor);
        //}

        public async Task LogoutUserAsync(string userId)
        {
            await _signInManager.SignOutAsync();
        }

        //public async Task<IEnumerable<User>?> GetAllUsers()
        //{
        //    return await _userManager.Users.ToListAsync();
        //}

        // 其他方法（Logout、VerifyEmail等）实现类似逻辑...
    }
}
