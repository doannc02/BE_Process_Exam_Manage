﻿using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Repository;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/exam-set")]
    [ApiController]
    public class ExamsetController : ControllerBase
    {
        private readonly IExamSetRepository _repository;
        private readonly CreateCommonResponse _createCommonResponse;

        public ExamsetController(IExamSetRepository repository)
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
                    return new CustomJsonResult(StatusCodes.Status403Forbidden, HttpContext, "Unauthorized!!");
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
            catch (Exception ex)
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
    }
}
