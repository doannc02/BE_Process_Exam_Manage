using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

            var yearResponse = _createCommon.CreateResponse("Thành công", HttpContext, academics);
            return Ok(yearResponse);
        }

        // GET detail of an academic_year
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailAcademicYearAsync([FromQuery] [Required] int id)
        {
            var academic = await _repository.GetDetailAcademicYearAsync(id);

            if (academic.status != 200)
                return new CustomJsonResult(academic.status, HttpContext, academic.message, academic.errors);

            var yearResponse = _createCommon.CreateResponse(academic.message, HttpContext, academic.data);
            return Ok(yearResponse);
        }

        // POST an academic_year
        [HttpPost]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PostAcademicYearAsync([FromBody] AcademicYearResponse year)
        {
            var yearAdd = await _repository.CreateAcademicYearAsync(year);

            if (yearAdd.status != 200)
                return new CustomJsonResult(yearAdd.status, HttpContext, yearAdd.message, yearAdd.errors);

            var response = _createCommon.CreateResponse(yearAdd.message, HttpContext, yearAdd.data);
            return Ok(response);
        }

        // PUT api/<AcademicYearController>/5
        [HttpPut]
        [Authorize(Roles = "Admin, Writer")]
        public async Task<IActionResult> PutAcademicYearAsync([FromBody] AcademicYearResponse year)
        {
            var yearUpdate = await _repository.UpdateAcademicYearAsync(year);

            if (yearUpdate.status != 200)
                return new CustomJsonResult(yearUpdate.status, HttpContext, yearUpdate.message, yearUpdate.errors);

            var response = _createCommon.CreateResponse(yearUpdate.message, HttpContext, yearUpdate.data);
            return Ok(response);
        }

        // DELETE api/<AcademicYearController>/5
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAcademicYearAsync([FromQuery] [Required] int id)
        {
            var yearDel = await _repository.DeleteAcademicYearAsync(id);

            if (yearDel.status != 200)
                return new CustomJsonResult(yearDel.status, HttpContext, yearDel.message, yearDel.errors);

            var response = _createCommon.CreateResponse(yearDel.message, HttpContext, yearDel.data);
            return Ok(response);
        }
    }
}