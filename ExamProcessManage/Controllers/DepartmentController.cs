using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/department")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentRepository _repository;
        private readonly CreateCommonResponse _createCommon;

        public DepartmentController(IDepartmentRepository departmentRepository)
        {
            _repository = departmentRepository;
            _createCommon = new CreateCommonResponse();
        }

        // GET: api/<DepartmentController>
        [HttpGet("list")]
        public async Task<IActionResult> GetListDepartmentAsync([FromQuery] QueryObject queryObject)
        {
            var listDepart = await _repository.GetListDepartmentAsync(queryObject);

            var response = _createCommon.CreateResponse("Thành công", HttpContext, listDepart);
            return Ok(response);
        }

        // GET api/<DepartmentController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailDepartmentAsync([FromQuery] [Required] int id)
        {
            var detailDepartment = await _repository.GetDetailDepartmentAsync(id);

            if (detailDepartment.status != 200)
                return new CustomJsonResult(detailDepartment.status, HttpContext, detailDepartment.message,
                    detailDepartment.errors);

            var response = _createCommon.CreateResponse(detailDepartment.message, HttpContext, detailDepartment.data);
            return Ok(response);
        }

        // POST api/<DepartmentController>
        [HttpPost]
        public async Task<IActionResult> PostDepartmentAsync([FromBody] DepartmentResponse department)
        {
            var newDepartment = await _repository.CreateDepartmentAsync(department);

            if (newDepartment.status != 200)
                return new CustomJsonResult(newDepartment.status, HttpContext, newDepartment.message,
                    newDepartment.errors);

            var response = _createCommon.CreateResponse(newDepartment.message, HttpContext, newDepartment.data);
            return Ok(response);
        }

        // PUT api/<DepartmentController>/5
        [HttpPut]
        public async Task<IActionResult> PutDepartmentAsync([FromBody] DepartmentResponse department)
        {
            var updateDepartment = await _repository.UpdateDepartmentAsync(department);

            if (updateDepartment.status != 200)
                return new CustomJsonResult(updateDepartment.status, HttpContext, updateDepartment.message,
                    updateDepartment.errors);

            var response = _createCommon.CreateResponse(updateDepartment.message, HttpContext, updateDepartment.data);
            return Ok(response);
        }

        // DELETE api/<DepartmentController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteDepartmentAsync([Required] int id)
        {
            var deleteDepartment = await _repository.DeleteDepartmentAsync(id);

            if (deleteDepartment.status != 200)
                return new CustomJsonResult(deleteDepartment.status, HttpContext, deleteDepartment.message,
                    deleteDepartment.errors);

            var response = _createCommon.CreateResponse(deleteDepartment.message, HttpContext, deleteDepartment.data);
            return Ok(response);
        }
    }
}