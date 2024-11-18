using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Repository;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static ExamProcessManage.RequestModels.NotificationRequest;

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

    [HttpGet("get-by-user-id")]
    public async Task<IActionResult> GetAllNotificationsByUserIdAsync()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");
        var notifications = await _repository.GetAllNotificationsByUserIdAsync(int.Parse(userId.Value));

        var response = _createResponse.CreateResponse("Thành công", HttpContext, notifications);
        return Ok(response);
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
    [HttpPut("update-state")]
    public async Task<IActionResult> UpdateNotificationsAsync([FromBody] UpdateNotificationStatusRequest request)
    {
        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return BadRequest("Notification IDs cannot be empty.");
        }

        int updatedCount = await _repository.UpdateNotificationAsync(request.NotificationIds, request.IsRead);

        return Ok(new
        {
            message = $"{updatedCount} notifications updated successfully."
        });
    }

    /// <summary>
    /// Xóa nhiều thông báo
    /// </summary>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteNotificationsAsync([FromBody] DeleteNotificationRequest request)
    {
        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return BadRequest("Notification IDs cannot be empty.");
        }

        int deletedCount = await _repository.DeleteNotificationAsync(request.NotificationIds);

        return Ok(new
        {
            message = $"{deletedCount} notifications deleted successfully."
        });
    }
}