using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces;

public interface INotificationRepository
{
    Task<PageResponse<NotificationDTO>> GetAllNotificationsAsync(QueryObject queryObject);
    Task<NotificationDTO?> GetNotificationByIdAsync(int notificationId);
    Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserIdAsync(int userId);
    Task<PageResponse<NotificationDTO>> GetAllNotificationsByDateAsync(DateTime date);
    Task<PageResponse<NotificationDTO>> GetAllNotificationsByDateAsync(DateTime startDate, DateTime endDate);
    Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserAndDateAsync(int userId, DateTime date);
    Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserAndDateAsync(int userId, DateTime start, DateTime endDate);
}