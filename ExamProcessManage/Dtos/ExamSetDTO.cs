using ExamProcessManage.Helpers;

namespace ExamProcessManage.Dtos
{
    public class ExamSetDTO
    {
        public ExamSetDTO()
        {
            exams = new HashSet<CommonObject>();
        }

        public int? id { get; set; }
        public string? name { get; set; }
        public string? department { get; set; }
        public string? major { get; set; }
        public int exam_quantity { get; set; }
        public string? description { get; set; }
        public string status { get; set; } = null!;
        public CommonObject course { get; set; }
        public CommonObject? proposal { get; set; }
        public virtual CommonObject? user { get; set; }
        public IEnumerable<CommonObject> exams { get; set; }
    }
}
