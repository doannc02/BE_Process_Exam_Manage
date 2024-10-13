using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Repository;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly CreateCommonResponse _createCommonResponse;
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _createCommonResponse = new CreateCommonResponse();
        }

        [HttpGet]
       // [Authorize(Roles = "Admin")]
        //[AllowAnonymous]
        [Route("/api/v1/user/list")]
        public async Task<IActionResult> GetListUserAsync([FromQuery] QueryObject query)
        {
            var users = await _userRepository.GetListUsersAsync(query);
            if (users != null && users.content != null && users.content.Any())
            {
                var response = _createCommonResponse.CreateResponse("Thành công", HttpContext, users);
                return Ok(response);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
       // [Authorize(Roles = "Admin")]
        //[AllowAnonymous]
        [Route("/api/v1/user/detail")]
        public async Task<IActionResult> GetUserDetail()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");
            if(userId == null)
            {
                return Unauthorized();
            }
            var baseResUser = await _userRepository.GetDetailUserAsync(int.Parse(userId.Value));
            if (baseResUser != null)
            {
                var response = _createCommonResponse.CreateResponse(baseResUser.message, HttpContext, baseResUser);
                return Ok(response);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
