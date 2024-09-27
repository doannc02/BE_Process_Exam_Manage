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

            if (listDepart.totalElements > 0)
            {
                var response = _createCommon.CreateResponse("success", HttpContext, listDepart);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
            }
        }

        // GET api/<DepartmentController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailDepartmentAsync([FromQuery][Required] int id)
        {
            var detailDepartment = await _repository.GetDetailDepartmentAsync(id);

            if (detailDepartment.data != null)
            {
                var response = _createCommon.CreateResponse(detailDepartment.message, HttpContext, detailDepartment.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, detailDepartment.message);
            }
        }

        // POST api/<DepartmentController>
        [HttpPost]
        public async Task<IActionResult> PostDepartmentAsync([FromBody] DepartmentResponse department)
        {
            if (department.id != 0 && department.name != "string")
            {
                var newDepartment = await _repository.CreateDepartmentAsync(department);

                if (newDepartment.data != null)
                {
                    var response = _createCommon.CreateResponse(newDepartment.message, HttpContext, newDepartment.data);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult(409, HttpContext, newDepartment.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // PUT api/<DepartmentController>/5
        [HttpPut]
        public async Task<IActionResult> PutDepartmentAsync([FromBody] DepartmentResponse department)
        {
            if (department.id != 0 && department.name != "string")
            {
                var updated = await _repository.UpdateDepartmentAsync(department);

                if (updated.data != null)
                {
                    var response = _createCommon.CreateResponse(updated.message, HttpContext, updated.data);
                    return Ok(response);
                }
                else if (updated.message.Contains("no changes"))
                {
                    return new CustomJsonResult(418, HttpContext, updated.message);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, updated.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // DELETE api/<DepartmentController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteDepartmentAsync([Required] int id)
        {
            var deleted = await _repository.DeleteDepartmentAsync(id);

            if (deleted.data != null)
            {
                var response = _createCommon.CreateResponse(deleted.message, HttpContext, deleted.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, deleted.message);
            }
        }
    }
}
