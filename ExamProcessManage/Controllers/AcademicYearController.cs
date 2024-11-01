using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/academicyear")]
    [ApiController]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearRepository _repository;
        private readonly CreateCommonResponse _createCommon;

        private const string Success = "Success";

        // Matches years between 2000 and 2099
        private const string YearPattern = @"^20\d{2}$";

        public AcademicYearController(IAcademicYearRepository repository)
        {
            _repository = repository;
            _createCommon = new CreateCommonResponse();
        }

        // GET: list of academic_year
        [HttpGet("list")]
        public async Task<IActionResult> GetListAcademicYearAsync([FromQuery] QueryObject queryObject)
        {
            try
            {
                var academics = await _repository.GetListAcademicYearAsync(queryObject);

                if (academics is { content: null })
                {
                    return new CustomJsonResult(500, HttpContext, "An internal server error occured");
                }

                var commonResponse = _createCommon.CreateResponse(Success, HttpContext, academics);
                return Ok(commonResponse);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // GET detail of an academic_year
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailAcademicYearAsync([FromQuery] [Required] int id)
        {
            try
            {
                var academic = await _repository.GetDetailAcademicYearAsync(id);

                if (academic is { data: null })
                {
                    return new CustomJsonResult(404, HttpContext, academic.message ?? "Not Found");
                }

                var yearResponse =
                    _createCommon.CreateResponse(academic.message ?? Success, HttpContext, academic.data);
                return Ok(yearResponse);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // POST an academic_year
        [HttpPost]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PostAcademicYearAsync([FromBody] AcademicYearResponse year)
        {
            try
            {
                if (year is not { id: > 0, start_year: > 0 } || year.start_year <= year.end_year ||
                    !Regex.IsMatch(year.start_year.ToString(), YearPattern))
                {
                    return new CustomJsonResult(400, HttpContext, "Invalid academic year");
                }

                var yearAdd = await _repository.CreateAcademicYearAsync(year);

                if (yearAdd is { data: null })
                {
                    return new CustomJsonResult(500, HttpContext, yearAdd.message ?? "Error");
                }

                var response =
                    _createCommon.CreateResponse(yearAdd.message ?? Success, HttpContext, yearAdd.data);
                return Ok(response);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // PUT api/<AcademicYearController>/5
        [HttpPut]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PutAcademicYearAsync([FromBody] AcademicYearResponse year)
        {
            try
            {
                if (year is not { id: > 0, start_year: > 0, end_year: > 0 } ||
                    year.start_year >= year.end_year ||
                    !Regex.IsMatch(year.start_year.ToString(), YearPattern) ||
                    !Regex.IsMatch(year.end_year.ToString(), YearPattern))
                {
                    return new CustomJsonResult(400, HttpContext, "Invalid academic year");
                }

                var yearUpdate = await _repository.UpdateAcademicYearAsync(year);

                if (yearUpdate is { data: null })
                {
                    return new CustomJsonResult(404, HttpContext, yearUpdate.message ?? "");
                }

                var response =
                    _createCommon.CreateResponse(yearUpdate.message ?? Success, HttpContext, yearUpdate.data);
                return Ok(response);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // DELETE api/<AcademicYearController>/5
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAcademicYearAsync([FromQuery] [Required] int id)
        {
            try
            {
                var yearDel = await _repository.DeleteAcademicYearAsync(id);

                if (yearDel is { data: null })
                {
                    return new CustomJsonResult(500, HttpContext, yearDel.message ?? "Error");
                }

                var response =
                    _createCommon.CreateResponse(yearDel.message ?? Success, HttpContext, yearDel.data);
                return Ok(response);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }
    }
}