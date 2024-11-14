using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ExamProcessManage.Controllers;

[Route("api/v1/exam-set")]
[ApiController]
public class ExamSetController : ControllerBase
{
    private readonly IExamSetRepository _repository;
    private readonly CreateCommonResponse _createResponse;

    public ExamSetController(IExamSetRepository repository)
    {
        _repository = repository;
        _createResponse = new CreateCommonResponse();
    }

    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> GetListExamSetAsync([FromQuery] RequestParamsExamSets paramsExamSets)
    {
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

        if (roleClaim == null || userId == null)
            return Forbid();

        var isAdmin = User.IsInRole("Admin");

        var pageResponse =
            await _repository.GetListExamSetAsync(!isAdmin ? int.Parse(userId.Value) : null, paramsExamSets);

        var response = _createResponse.CreateResponse("Thành công", HttpContext, pageResponse);
        return Ok(response);
    }

    [HttpGet]
    [Route("detail")]
    public async Task<IActionResult> GetDetailExamSetAsync(int examSetId)
    {
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

        if (roleClaim == null || userId == null)
            return Forbid();

        var detailExamSet =
            await _repository.GetDetailExamSetAsync(examSetId);

        if (detailExamSet.status != 200)
            return new CustomJsonResult(detailExamSet.status, HttpContext, detailExamSet.message,
                detailExamSet.errors);

        var commonResponse = _createResponse.CreateResponse(detailExamSet.message, HttpContext, detailExamSet.data);
        return Ok(commonResponse);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExamSetAsync([FromBody] ExamSetDTO examSetDto)
    {
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

        if (roleClaim == null || userId == null || roleClaim.Value == "Admin")
            return Forbid();

        var examSetId = await _repository.CreateExamSetAsync(int.Parse(userId.Value), examSetDto);

        if (examSetId.status != 200)
            return new CustomJsonResult(examSetId.status, HttpContext, examSetId.message, examSetId.errors);

        var response = _createResponse.CreateResponse(examSetId.message, HttpContext, examSetId.data);
        return Ok(response);
    }

    [HttpPut]
    public async Task<IActionResult> PutExamSetAsync([FromBody] ExamSetDTO examSetDto)
    {
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

        if (roleClaim == null || userId == null)
            return Forbid();

        var updatedExamSet =
            await _repository.UpdateExamSetAsync(int.Parse(userId.Value), examSetDto, roleClaim.Value == "Admin");
        if (updatedExamSet.status != 200)
            return new CustomJsonResult(updatedExamSet.status, HttpContext, updatedExamSet.message,
                updatedExamSet.errors);

        var response = _createResponse.CreateResponse(updatedExamSet.message, HttpContext, updatedExamSet.data);
        return Ok(response);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteExamSetAsync([FromQuery] [Required] int examSetId, bool isIncludeExam = false)
    {
        var user = User.Claims.FirstOrDefault(c => c.Type == "userId");
        if (user == null)
            return Forbid();

        var delExamSet = await _repository.DeleteExamSetAsync(int.Parse(user.Value), examSetId, isIncludeExam);
        if (delExamSet.status != 200)
            return new CustomJsonResult(delExamSet.status, HttpContext, delExamSet.message, delExamSet.errors);

        var response = _createResponse.CreateResponse(delExamSet.message, HttpContext, delExamSet.data);
        return Ok(response);
    }
}