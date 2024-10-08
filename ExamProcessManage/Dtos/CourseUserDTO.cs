using ExamProcessManage.Helpers;

namespace ExamProcessManage.Dtos
{
    public class CourseUserDTO
    {
        public ulong? Id { get; set; }
        public ulong CourseId { get; set; }
        public ulong UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
