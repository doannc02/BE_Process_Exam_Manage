using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PageResponse<NotificationDTO>> GetAllNotificationsAsync(QueryObject queryObject)
    {
        var baseQuery = _context.Notifications.AsQueryable();

        if (!string.IsNullOrEmpty(queryObject.search))
        {
            baseQuery = baseQuery.Where(n =>
                n.Title.Contains(queryObject.search) || n.Message.Contains(queryObject.search));
        }

        if (queryObject.userId is { } or > 0)
        {
            baseQuery = baseQuery.Where(n => n.UserId == queryObject.userId);
        }

        if (!string.IsNullOrEmpty(queryObject.sort))
        {
            baseQuery = queryObject.sort.ToLower() switch
            {
                "title" => baseQuery.OrderBy(n => n.Title), // Sắp xếp theo tiêu đề tăng dần
                "title_desc" => baseQuery.OrderByDescending(n => n.Title), // Sắp xếp theo tiêu đề giảm dần
                "created_at" => baseQuery.OrderBy(n => n.CreatedAt), // Sắp xếp theo ngày tạo tăng dần
                "created_at_desc" => baseQuery.OrderByDescending(n => n.CreatedAt), // Sắp xếp theo ngày tạo giảm dần
                _ => baseQuery // Trả về truy vấn gốc nếu không nhận diện được giá trị
            };
        }

        var totalCount = await baseQuery.CountAsync();
        var listNotification = new List<NotificationDTO>();

        if (totalCount == 0)
        {
            return new PageResponse<NotificationDTO>
            {
                content = listNotification,
                totalElements = totalCount,
                totalPages = 0,
                size = queryObject.size,
                page = queryObject.page,
                numberOfElements = listNotification.Count
            };
        }

        var notifications = await baseQuery
            .Skip((queryObject.page - 1) * queryObject.size)
            .Take(queryObject.size)
            .ToListAsync();

        var userAvatars = new Dictionary<int, string>();
        var userIds = notifications.Select(n => n.UserId).Distinct().ToList();

        var users = await _context.Users.Where(u => userIds.Contains((int)u.Id))
            .Select(u => new { u.Id, u.AvatarPath })
            .ToListAsync();

        foreach (var user in users)
        {
            userAvatars[(int)user.Id] = user.AvatarPath;
        }

        listNotification.AddRange(notifications.Select(n => new NotificationDTO
        {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            avatar = userAvatars[n.UserId],
            user_id = n.UserId,
            created_at = n.CreatedAt,
            is_read = n.IsRead
        }));

        return new PageResponse<NotificationDTO>
        {
            content = listNotification,
            totalElements = totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
            size = queryObject.size,
            page = queryObject.page,
            numberOfElements = listNotification.Count
        };
    }

    public Task<NotificationDTO?> GetNotificationByIdAsync(int notificationId)
    {
        throw new NotImplementedException();
    }

    public Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserIdAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<PageResponse<NotificationDTO>> GetAllNotificationsByDateAsync(DateTime date)
    {
        throw new NotImplementedException();
    }

    public Task<PageResponse<NotificationDTO>> GetAllNotificationsByDateAsync(DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException();
    }

    public Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserAndDateAsync(int userId, DateTime date)
    {
        throw new NotImplementedException();
    }

    public Task<PageResponse<NotificationDTO>> GetAllNotificationsByUserAndDateAsync(int userId, DateTime start,
        DateTime endDate)
    {
        throw new NotImplementedException();
    }
}