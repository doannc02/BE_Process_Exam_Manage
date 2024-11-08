using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly IUserService _userService;

        public AccountController(ITokenService tokenService, IUserService userService)
        {
            _userService = userService;
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("forbidden")]
        public IActionResult Forbidde2n()
        {
            //  return Forbid();
            return Ok(new
            {
                message = "Admin"
            });
        }
        [Authorize(Roles = "Admin")]
        // [AllowAnonymous]
        [HttpGet("unAuthenticated")]
        public IActionResult Unauthenticated()
        {
            // return Unauthorized();
            return Ok(new
            {
                status = 400, 
                message = "Admin test 2"
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var valid = _userService.Authenticate(loginDto);

            if (valid == null)
            {
                return new CustomJsonResult(500, HttpContext, $"Server error", new() {
                    new() { message ="Tài khoản không tồn tại hoặc chưa được gán với giảng viên nào!!"  } });
            }

            if (valid.accessToken == null)
            {
                return new CustomJsonResult(500, HttpContext, $"Server error", new() {
                    new() { message ="Mật khẩu không chính xác!!!" , field = "password" } });
            }
            return Ok(valid);
        }

    }

}
