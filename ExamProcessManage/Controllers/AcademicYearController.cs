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

        public AcademicYearController(IAcademicYearRepository repository)
        {
            _repository = repository;
            _createCommon = new CreateCommonResponse();
        }

        // GET: list of academic_year
        [HttpGet("list")]
        public async Task<IActionResult> GetListAcademicYearAsync([FromQuery] QueryObject queryObject)
        {
            var academics = await _repository.GetListAcademicYearAsync(queryObject);

            if (academics.content.Any())
            {
                var commonResponse = _createCommon.CreateResponse("success", HttpContext, academics);
                return Ok(commonResponse);
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
            }
        }

        // GET detail of an academic_year
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailAcademicYearAsync([FromQuery][Required] int id)
        {
            var academic = await _repository.GetDetailAcademicYearAsync(id);
            if (academic.data != null)
            {
                var yearResponse = _createCommon.CreateResponse(academic.message, HttpContext, academic.data);
                return Ok(yearResponse);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, academic.message);
            }
        }

        // POST an academic_year
        [HttpPost]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PostAcdemicYearAsync([FromBody] AcademicYearResponse year)
        {
            // Matches years between 2000 and 2099
            string yearPattern = @"^20\d{2}$";

            if (year.year_id > 0 && year.start_year > 0 && year.start_year > year.end_year &&
                Regex.IsMatch(year.start_year.ToString(), yearPattern))
            {
                var yearAdd = await _repository.CreateAcademicYearAsync(year);

                if (yearAdd.data != null)
                {
                    var response = _createCommon.CreateResponse(yearAdd.message, HttpContext, yearAdd.data);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult(409, HttpContext, yearAdd.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // PUT api/<AcademicYearController>/5
        [HttpPut]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PutAcademicYearAsync([FromBody] AcademicYearResponse year)
        {
            // Matches years between 2000 and 2099
            string yearPattern = @"^20\d{2}$";

            if (year.year_id > 0 && year.start_year > 0 && year.end_year > 0 &&
                year.start_year < year.end_year &&
                Regex.IsMatch(year.start_year.ToString(), yearPattern) &&
                Regex.IsMatch(year.end_year.ToString(), yearPattern))
            {
                var yearUpdate = await _repository.UpdateAcademicYearAsync(year);

                if (yearUpdate.data != null)
                {
                    var response = _createCommon.CreateResponse(yearUpdate.message, HttpContext, yearUpdate.data);
                    return Ok(response);
                }
                else if (yearUpdate.message.Contains("no changes"))
                {
                    return new CustomJsonResult(418, HttpContext, yearUpdate.message);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, yearUpdate.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // DELETE api/<AcademicYearController>/5
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAcademicYearAsync([FromQuery][Required] int id)
        {
            var yearDel = await _repository.DeleteAcademicYearAsync(id);

            if (yearDel.data != null)
            {
                var response = _createCommon.CreateResponse(yearDel.message, HttpContext, yearDel.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, yearDel.message);
            }
        }
    }
}
