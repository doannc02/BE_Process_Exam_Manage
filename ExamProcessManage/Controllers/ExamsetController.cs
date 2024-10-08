﻿using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.Repository;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/exam-set")]
    [ApiController]
    [AllowAnonymous]
    public class ExamSetController : ControllerBase
    {
        private readonly IExamSetRepository _repository;
        private readonly CreateCommonResponse _createCommonResponse;

        public ExamSetController(IExamSetRepository repository)
        {
            _repository = repository;
            _createCommonResponse = new CreateCommonResponse();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetList([FromQuery] RequestParamsExamSets req)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null)
                {
                    return Forbid();
                }
                else
                {
                    if (roleClaim.Value == "Admin")
                    {
                        var t = await _repository.GetListExamSetAsync(null, req);
                        if (t != null)
                        {
                            var res = _createCommonResponse.CreateResponse("Thành công", HttpContext, t);
                            return Ok(res);
                        }
                        return new CustomJsonResult(500, HttpContext, $"Server error");
                    }
                    else
                    {

                        var examSets = await _repository.GetListExamSetAsync(int.Parse(userId.Value), req);

                        if (examSets != null)
                        {
                            var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, examSets);
                            return Ok(commonResponse);
                        }
                        else
                        {
                            return new CustomJsonResult(500, HttpContext, "Error!");
                        }
                    }
                }


            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Server error!!");
            }
        }

        [HttpGet]
        [Route("detail")]
        public async Task<IActionResult> GetDetail(int req)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null)
                {
                    return new CustomJsonResult(StatusCodes.Status403Forbidden, HttpContext, "Fib!!");
                }
                else
                {
                    if (roleClaim.Value == "Admin")
                    {
                        var t = await _repository.GetDetailExamSetAsync(null, req);
                        if (t.data == null && t.message == "403")
                        {
                            return new CustomJsonResult(StatusCodes.Status403Forbidden, HttpContext, "Unauthorized!");
                        }
                        if (t.data != null)
                        {
                            var res = _createCommonResponse.CreateResponse("Thành công", HttpContext, t);
                            return Ok(res);
                        }
                        return new CustomJsonResult(500, HttpContext, $"Server error");

                    }
                    else
                    {
                        var examSets = await _repository.GetDetailExamSetAsync(int.Parse(userId.Value), req);

                        if (examSets != null)
                        {
                            var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, examSets);
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

        [HttpPost]
        public async Task<IActionResult> CreateExamSet([FromBody] ExamSetDTO examSetDTO)
        {
            try
            {
                var uID = User.Claims.FirstOrDefault(c => c.Type == "userId");
                if (uID != null)
                {
                    var res = await _repository.CreateExamSetAsync(int.Parse(uID.Value), examSetDTO);

                    if (res != null) { return Ok(res); }
                    else return new CustomJsonResult(500, HttpContext, "Error");
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "Server error: " + ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutExamSetAsync([FromBody] ExamSetDTO examSet)
        {
            try
            {
                var uID = User.Claims.FirstOrDefault(c => c.Type == "userId");
                if (uID != null)
                {
                    var updatedExamSet = await _repository.UpdateExamSetAsync(int.Parse(uID.Value), examSet);

                    if (updatedExamSet != null)
                    {
                        if (updatedExamSet.status != null && updatedExamSet.errors != null && updatedExamSet.errors.Any())
                        {
                            return new CustomJsonResult((int)updatedExamSet.status, HttpContext, updatedExamSet.message, (List<ErrorDetail>?)updatedExamSet.errors);
                        }
                        else
                        {
                            var response = _createCommonResponse.CreateResponse(updatedExamSet.message, HttpContext, updatedExamSet.data);
                            return Ok(response);
                        }
                    }
                    else
                    {
                        return new CustomJsonResult(400, HttpContext, "Error");
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "Server error: " + ex.Message + "\n" + ex.InnerException);
            }
        }

        [HttpPut("update-state")]
        public async Task<IActionResult> UpdateStateAsync([FromQuery][Required] int examSetId,[Required] string status, string? comment)
        {
            try
            {
                var updState = await _repository.UpdateStateAsync(examSetId, status, comment);
                if (updState != null && updState.data != null)
                {
                    var response = _createCommonResponse.CreateResponse(updState.message, HttpContext, updState.data);
                    return Ok(response);
                }
                else if (updState != null)
                {
                    return new CustomJsonResult((int)updState.status, HttpContext, updState.message, (List<ErrorDetail>)updState.errors);
                }
                else
                {
                    return new CustomJsonResult(400, HttpContext, "Error");
                }
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, $"Internal Server Error: {ex.Message}: {ex.InnerException}" );
            }
        }

        [HttpPut("remove-child")]
        public async Task<IActionResult> RemoveChildAsync([FromQuery] int parentId, int childId, string? comment)
        {
            return NotFound();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteExamSetAsync([FromQuery] int examSetId)
        {
            return NotFound();
        }
    }
}
