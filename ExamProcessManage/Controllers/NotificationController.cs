using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamProcessManage.Controllers;

[Route("api/v1/notification")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly INotificationRepository _repository;
    private readonly CreateCommonResponse _createResponse;

    public NotificationController(INotificationRepository repository)
    {
        _repository = repository;
        _createResponse = new CreateCommonResponse();
    }

    [HttpGet("get-all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllNotificationsAsync([FromQuery] QueryObject queryObject)
    {
        var notifications = await _repository.GetAllNotificationsAsync(queryObject);

        var response = _createResponse.CreateResponse("Thành công", HttpContext, notifications);
        return Ok(response);
    }

    [HttpGet("get-by-id/{notificationId:int}")]
    public Task<IActionResult> GetNotificationByIdAsync(int notificationId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("get-all-by-user/{userId:int}")]
    public Task<IActionResult> GetAllNotificationsByUserIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("get-all-by-date/{date:datetime}")]
    public Task<IActionResult> GetAllNotificationsByDateAsync(DateTime date)
    {
        throw new NotImplementedException();
    }

    [HttpGet("get-all-by-date-range")]
    public Task<IActionResult> GetAllNotificationsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    [HttpGet("get-all-by-user-and-date/{userId:int}/{date:datetime}")]
    public Task<IActionResult> GetAllNotificationsByUserAndDateAsync(int userId, DateTime date)
    {
        throw new NotImplementedException();
    }

    [HttpGet("get-all-by-user-and-date-range/{userId:int}")]
    public Task<IActionResult> GetAllNotificationsByUserAndDateRangeAsync(int userId, DateTime startDate,
        DateTime endDate)
    {
        throw new NotImplementedException();
    }
}