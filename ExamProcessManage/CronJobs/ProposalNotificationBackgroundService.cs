using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ProposalNotificationBackgroundService : BackgroundService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationRepository _notificationRepository;

    public ProposalNotificationBackgroundService(
        ApplicationDbContext context,
        INotificationRepository notificationRepository)
    {
        _context = context;
        _notificationRepository = notificationRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Lấy các đề xuất có thời gian nhỏ hơn 10 ngày
                var proposals = await _context.Proposals
                    .Where(p => p.EndDate.HasValue &&
                                p.EndDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)) &&
                                p.EndDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                    .ToListAsync(stoppingToken);

                // Tạo danh sách thông báo cho tất cả các đề xuất
                var notifications = proposals.Select(proposal =>
                {
                    var teacherProp = _context.TeacherProposals
                        .FirstOrDefault(t => t.ProposalId == proposal.ProposalId);
                    if (teacherProp != null)
                    {
                        return new NotificationDTO
                        {
                            title = "Đề xuất sắp hết hạn",
                            message = $"Đề xuất {proposal.PlanCode} còn 10 ngày!! Hãy sắp xếp thời gian để hoàn thành!",
                            user_id = (int)teacherProp.UserId
                        };
                    }
                    return null;
                })
                .Where(notification => notification != null) // Loại bỏ thông báo null
                .ToList();

                // Thêm các thông báo vào cơ sở dữ liệu
                var addNotificationTasks = notifications.Select(notification =>
                    _notificationRepository.AddNotificationAsync(notification)).ToList();

                await Task.WhenAll(addNotificationTasks); // Chạy tất cả các tác vụ bất đồng bộ đồng thời

            }
            catch (Exception ex)
            {
                // Log lỗi nếu có
                // _logger.LogError($"Error occurred while checking proposals: {ex.Message}");
            }

            // Chạy lại mỗi 24 giờ
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
