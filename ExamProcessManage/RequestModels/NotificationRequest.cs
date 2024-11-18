namespace ExamProcessManage.RequestModels
{
    public class NotificationRequest
    {
        public class UpdateNotificationStatusRequest
        {
            public List<int> NotificationIds { get; set; }
            public bool IsRead { get; set; }  // Trạng thái mới
        }

        public class DeleteNotificationRequest
        {
            public List<int> NotificationIds { get; set; }  // Danh sách ID thông báo cần xóa
        }

    }
}
