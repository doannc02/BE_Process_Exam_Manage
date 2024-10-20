﻿using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
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
                            var res = _createResponse.CreateResponse("Thành công", HttpContext, t);
                            return Ok(res);
                        }
                        return new CustomJsonResult(500, HttpContext, $"Server error");
                    }
                    else
                    {

                        var examSets = await _repository.GetListExamSetAsync(int.Parse(userId.Value), req);

                        if (examSets != null)
                        {
                            var commonResponse = _createResponse.CreateResponse("success", HttpContext, examSets);
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
                    return Forbid();
                }
                else
                {
                    if (roleClaim.Value == "Admin")
                    {
                        var t = await _repository.GetDetailExamSetAsync(null, req);
                        if (t.data == null && t.message == "403")
                        {
                            return Forbid();
                        }
                        if (t.data != null)
                        {
                            var res = _createResponse.CreateResponse("Thành công", HttpContext, t);
                            return Ok(res);
                        }
                        return new CustomJsonResult(500, HttpContext, $"Server error");

                    }
                    else
                    {
                        var examSets = await _repository.GetDetailExamSetAsync(int.Parse(userId.Value), req);

                        if (examSets != null)
                        {
                            var commonResponse = _createResponse.CreateResponse("success", HttpContext, examSets);
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
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null)
                {
                    return Forbid();
                }
                if (userId != null)
                {
                    var res = await _repository.CreateExamSetAsync(int.Parse(userId.Value), examSetDTO);

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
                        if (updatedExamSet.status != null || updatedExamSet.status == 500 && updatedExamSet.errors != null && updatedExamSet.errors.Any())
                        {
                            return new CustomJsonResult((int)updatedExamSet.status, HttpContext, updatedExamSet.message, updatedExamSet.errors);
                        }
                        else
                        {
                            var response = _createResponse.CreateResponse(updatedExamSet.message, HttpContext, updatedExamSet.data);
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

        [HttpDelete]
        public async Task<IActionResult> DeleteExamSetAsync([FromQuery][Required] int examSetId, bool examSetOnly = true)
        {
            try
            {
                var user = User.Claims.FirstOrDefault(c => c.Type == "userId");
                if (user != null)
                {
                    var delExamSet = await _repository.DeleteExamSetAsync(int.Parse(user.Value), examSetId, examSetOnly);
                    if (delExamSet.data != null)
                    {
                        var response = _createResponse.CreateResponse(delExamSet.message, HttpContext, delExamSet.data);
                        return Ok(response);
                    }
                    else return new CustomJsonResult((int)delExamSet.status, HttpContext, delExamSet.message, delExamSet.errors);
                }
                else return new CustomJsonResult(401, HttpContext, string.Empty);
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Server error");
            }
        }
    }
}
