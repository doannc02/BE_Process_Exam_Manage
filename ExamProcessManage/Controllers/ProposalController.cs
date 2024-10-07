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
    [AllowAnonymous]
    public class ProposalController : ControllerBase
    {
        private readonly IProposalRepository _proposalRepository;
        private readonly CreateCommonResponse _createCommonResponse;
        public ProposalController(IProposalRepository proposalRepository)
        {
            _createCommonResponse = new CreateCommonResponse();
            _proposalRepository = proposalRepository;
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetListAcademicYearAsync([FromQuery] QueryObject queryObject)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null)
                {
                    return new CustomJsonResult(401, HttpContext, "Unauthorized!!");
                }
                else
                {
                    if (roleClaim.Value == "Admin")
                    {
                        var proposals = await _proposalRepository.GetListProposalsAsync(null, queryObject);

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

                        var proposals = await _proposalRepository.GetListProposalsAsync(int.Parse(userId.Value), queryObject);

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
                return new CustomJsonResult(500, HttpContext, "Server error!!");
            }
        }

        [HttpGet]
        [Route("detail")]
        public async Task<IActionResult> GetDetailAcademicYearAsync(int id)
        {
            var proposals = await _proposalRepository.GetDetailProposalAsync(id);

            if (proposals != null)
            {
                var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, proposals);
                return Ok(commonResponse);
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
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
                    var response = await _proposalRepository.UpdateStateProposalAsync(proposalId, newState);

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
