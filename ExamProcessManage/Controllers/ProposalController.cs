using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
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
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userIdClaim == null)
                    return Forbid();

                var userId = int.Parse(userIdClaim.Value);
                var isAdmin = roleClaim.Value == "Admin";

                // Nếu là Admin nhưng user.id == 0, trả về lỗi
                if (isAdmin && (proposal.user?.id ?? 0) == 0)
                    return BadRequest(new { status = 400, title = "Admin must specify a valid user ID", field = "proposal.user" });

                // Check if the user is not an Admin but attempts to create a proposal for another user
                if (!isAdmin && proposal.user?.id > 0 && proposal.user.id != userId)
                    return Forbid();

                // Use the correct userId for proposal creation
                var targetUserId = isAdmin && proposal.user?.id > 0 ? proposal.user.id : userId;

                // Create proposal
                var newProposal = await _repository.CreateProposalAsync(targetUserId, proposal);

                // Return appropriate response
                if (newProposal.data != null)
                    return Ok(_createCommonResponse.CreateResponse("Success", HttpContext, newProposal.data));
                else
                    return new CustomJsonResult((int)newProposal.status, HttpContext, newProposal.message, newProposal.errors);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { status = 400, message = $"Bad request: {ex.Message}" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { status = 500, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "An error occurred: " + ex.Message + "\n" + ex.InnerException);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateStateProposalAsync([FromQuery][Required] int proposalId, [Required] string newState)
        {
            try
            {
                var status = new List<string> { "in_progress", "pending_approval", "approved", "rejected" };

                if (status.Contains(newState))
                {
                    var response = await _repository.UpdateStateProposalAsync(proposalId, newState);

                    if (response != null) return Ok(response);
                    else return new CustomJsonResult(500, HttpContext, "Error");
                }
                else
                {
                    return new CustomJsonResult(400, HttpContext, "newState không hợp lệ");
                }
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Server error");
            }
        }
    }
}
