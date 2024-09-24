using ExamProcessManage.Helpers;

namespace ExamProcessManage.Dtos
{
    public class UserDTO
    {
        public string id { get; set; }
        public string fullname { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string avatarPath { get; set; }
        public string createAt { get; set; }
        public string updatedAt { get; set; }
        public string deleteAt { get; set; }
        public CommonObject role { get; set; }
    }
}
