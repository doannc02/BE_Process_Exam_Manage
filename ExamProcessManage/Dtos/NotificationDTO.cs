namespace ExamProcessManage.Dtos;

public class NotificationDTO
{
    public int? id { get; set; }
    public int user_id { get; set; }
    public string title { get; set; }
    public string message { get; set; }
    public string avatar { get; set; }
    public DateTime created_at { get; set; } = DateTime.Now;
    public bool is_read { get; set; } = false;
}