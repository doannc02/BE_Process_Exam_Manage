using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetProposalAsync([FromQuery] QueryObjectProposal queryObject)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (roleClaim == null || userId == null)
                Forbid();

            var proposals =
                await _repository.GetListProposalsAsync(roleClaim.Value == "Admin" ? null : int.Parse(userId.Value),
                    queryObject);

            var commonResponse =
                _createCommonResponse.CreateResponse("success", HttpContext, proposals);
            return Ok(commonResponse);
        }

        [HttpGet]
        [Route("detail")]
        public async Task<IActionResult> GetProposalAsync(int id)
        {
            var proposal = await _repository.GetDetailProposalAsync(id);

            if (proposal.status != 200)
                return new CustomJsonResult(proposal.status, HttpContext, proposal.message, proposal.errors);

            var commonResponse = _createCommonResponse.CreateResponse(proposal.message, HttpContext, proposal.data);
            return Ok(commonResponse);
        }

        [HttpPost]
        public async Task<IActionResult> PostProposalAsync([FromBody] ProposalDTO proposal)
        {
            var errorList = new List<ErrorDetail>();

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
            if (isAdmin && (proposal.user.id ?? 0) == 0)
            {
                errorList.Add(new ErrorDetail
                {
                    field = "user",
                    message = "Dữ liệu không hợp lệ"
                });
                return new CustomJsonResult(500, HttpContext, "Null proposal", errorList);
            }

            // Nếu không phải admin, bỏ qua proposal.user.id và sử dụng userId hiện tại
            var targetUserId = isAdmin ? proposal.user.id ?? userId : userId;

            // Tạo proposal cho user xác định
            var newProposal = await _repository.CreateProposalAsync(targetUserId, proposal, isAdmin ? "Admin" : "User");

            // Trả về kết quả phù hợp
            if (newProposal.status != 200)
                return new CustomJsonResult(newProposal.status, HttpContext, newProposal.message, newProposal.errors);

            var response = _createCommonResponse.CreateResponse("Success", HttpContext, newProposal.data);
            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProposalAsync([FromBody] ProposalDTO proposal)
        {
            var errorList = new List<ErrorDetail>();

            // Lấy thông tin quyền (role) và userId từ các claim
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

            // Nếu không tìm thấy role hoặc userId, trả về lỗi
            if (roleClaim == null || userIdClaim == null)
                return Forbid();

            // Chuyển userId từ string thành int
            var userId = int.Parse(userIdClaim.Value);
            var isAdmin = roleClaim.Value == "Admin";

            var findProp = await _repository.GetDetailProposalAsync((int)proposal.id);
            if (findProp == null)
                return new CustomJsonResult(findProp.status, HttpContext, findProp.message, errorList);

            if (findProp.data.user.id != userId && !isAdmin)
                return Forbid();

            var upProposal = await _repository.UpdateProposalAsync(int.Parse(userIdClaim.Value), proposal);

            // Trả về kết quả phù hợp
            if (upProposal.data != null)
                return Ok(_createCommonResponse.CreateResponse("Success", HttpContext, upProposal.data));

            return new CustomJsonResult(500, HttpContext, upProposal.message, upProposal.errors);
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProposalAsync([Required] int id, bool withExamSet = false,
            bool withExam = false)
        {
            var delProposal = await _repository.DeleteProposalAsync(id, withExamSet, withExam);
            if (delProposal.status == 200)
                return new CustomJsonResult(delProposal.status, HttpContext, delProposal.message, delProposal.errors);

            var response = _createCommonResponse.CreateResponse(delProposal.message, HttpContext, delProposal.data);
            return Ok(response);
        }
    }
}