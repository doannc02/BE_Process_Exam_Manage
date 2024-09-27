using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Repository;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/proposal")]
    [Controller]
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
        [AllowAnonymous]
        [Route("list")]
        public async Task<IActionResult> GetListAcademicYearAsync([FromQuery] QueryObject queryObject)
        {
            var proposals = await _proposalRepository.GetListProposalsAsync(queryObject);

            if (proposals.content.Any())
            {
                var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, proposals);
                return Ok(commonResponse);
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
            }
        }

        [HttpGet]
        [AllowAnonymous]
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
    }
}
