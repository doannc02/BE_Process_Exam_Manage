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
                var err = new ErrorCodes
                {
                    code = "401",
                    message = "Không tìm thấy tài khoản trong hệ thống!!",
                };
                var result = new ErrorMessage<ErrorCodes>
                {
                    errorCodes = new List<ErrorCodes> { err }

                };
                return Ok(result);
            }

            if (valid.accessToken == null)
            {
                var err = new ErrorCodes
                {
                    code = "password",
                    message = "Mật khẩu không chính xác!!!",
                };

                var result = new ErrorMessage<ErrorCodes>
                {
                    errorCodes = new List<ErrorCodes> { err }

                };
                return Ok(result);
            }
            return Ok(valid);

        }

    }

}
