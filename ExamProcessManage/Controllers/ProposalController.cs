using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/proposal")]
    [Controller]
    public class ProposalController : ControllerBase
    {
        private readonly IProposalRepository _repository;
        private readonly CreateCommonResponse _createCommonResponse;
        public ProposalController(IProposalRepository proposalRepository)
        {
            _createCommonResponse = new CreateCommonResponse();
            _repository = proposalRepository;
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetProposalAsync([FromQuery] QueryObject queryObject)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null) { return new CustomJsonResult(401, HttpContext, "Unauthorized!!"); }
                else
                {
                    if (roleClaim.Value == "Admin")
                    {
                        var proposals = await _repository.GetListProposalsAsync(null, queryObject);

                        if (proposals != null)
                        {
                            var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, proposals);
                            return Ok(commonResponse);
                        }
                        else
                        {
                            return new CustomJsonResult(500, HttpContext, "Error!");
                        }
                    }
                    else
                    {
                        var proposals = await _repository.GetListProposalsAsync(int.Parse(userId.Value), queryObject);
                        if (proposals != null)
                        {
                            var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, proposals);
                            return Ok(commonResponse);
                        }
                        else
                        {
                            return new CustomJsonResult(500, HttpContext, "Error!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "Server error!!" + ex.Message + "\n" + ex.InnerException);
            }
        }

        [HttpGet]
        [Route("detail")]
        public async Task<IActionResult> GetProposalAsync(int id)
        {
            var proposals = await _repository.GetDetailProposalAsync(id);

            if (proposals != null)
            {
                var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, proposals);
                return Ok(commonResponse);
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "Bad request");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostProposalAsync([FromBody] ProposalDTO proposal)
        {
            if (proposal == null)
                return new CustomJsonResult(400, HttpContext, "Null proposal");

            try
            {
                // Lấy thông tin quyền (role) và userId từ các claim
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

                // Nếu không tìm thấy role hoặc userId, trả về lỗi
                if (roleClaim == null || userIdClaim == null)
                    return Forbid();

                // Chuyển userId từ string thành int
                var userId = int.Parse(userIdClaim.Value);
                var isAdmin = roleClaim.Value == "Admin"; // Xác định có phải admin không

                // Nếu là admin nhưng proposal.user.id không hợp lệ (tức là bằng 0), trả về lỗi
                if (isAdmin && (proposal.user?.id ?? 0) == 0)
                    return BadRequest(new
                    {
                        status = 400,
                        title = "Admin must specify a valid user ID",
                        field = "user"
                    });

                // Nếu không phải admin, bỏ qua proposal.user.id và sử dụng userId hiện tại
                var targetUserId = isAdmin ? (proposal.user?.id ?? userId) : userId;

                // Tạo proposal cho user xác định
                var newProposal = await _repository.CreateProposalAsync(targetUserId, proposal);

                // Trả về kết quả phù hợp
                if (newProposal.data != null)
                    return Ok(_createCommonResponse.CreateResponse("Success", HttpContext, newProposal.data));
                else
                    return new CustomJsonResult((int)newProposal.status, HttpContext, newProposal.message, newProposal.errors);
            }
            catch (ArgumentException ex)
            {
                // Bắt lỗi và trả về BadRequest nếu có exception liên quan đến tham số không hợp lệ
                return BadRequest(new { status = 400, message = $"Bad request: {ex.Message}" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProposalAsync([FromBody] ProposalDTO proposal)
        {
            if (proposal == null)
                return new CustomJsonResult(400, HttpContext, "Null proposal");

            try
            {
                // Lấy thông tin quyền (role) và userId từ các claim
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

                // Nếu không tìm thấy role hoặc userId, trả về lỗi
                if (roleClaim == null || userIdClaim == null)
                    return Forbid();

                // Chuyển userId từ string thành int
                var userId = int.Parse(userIdClaim.Value);
                var isAdmin = roleClaim.Value == "Admin"; // Xác định có phải admin không

                var findProp = await _repository.GetDetailProposalAsync((int)proposal.id);
                if (findProp == null) return new CustomJsonResult(500, HttpContext, "Khong tim thay de xuat");
                if (findProp.data?.user.id != userId) return Forbid();
                var newProposal = await _repository.UpdateProposalAsync(proposal);

                // Trả về kết quả phù hợp
                if (newProposal.data != null)
                    return Ok(_createCommonResponse.CreateResponse("Success", HttpContext, newProposal.data));
                else
                    return new CustomJsonResult((int)newProposal.status, HttpContext, newProposal.message, newProposal.errors);
            }
            catch (ArgumentException ex)
            {
                // Bắt lỗi và trả về BadRequest nếu có exception liên quan đến tham số không hợp lệ
                return BadRequest(new { status = 400, message = $"Bad request: {ex.Message}" });
            }
        }

        [HttpPut("update-state")]
        public async Task<IActionResult> UpdateStateProposalAsync([Required] int proposalId, [Required] string status, string comment)
        {
            try
            {
                var updateStatus = await _repository.UpdateStateProposalAsync(proposalId, status, comment);
                if (updateStatus != null && updateStatus.data != null)
                {
                    var response = _createCommonResponse.CreateResponse(updateStatus.message, HttpContext, updateStatus.data);
                    return Ok(response);
                }
                else if (updateStatus != null)
                    return new CustomJsonResult((int)updateStatus.status, HttpContext, updateStatus.message, updateStatus.errors);
                else
                    return new CustomJsonResult(400, HttpContext, "Unable to update state");
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}
