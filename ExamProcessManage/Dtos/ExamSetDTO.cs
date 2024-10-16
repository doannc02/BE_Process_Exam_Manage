﻿using ExamProcessManage.Helpers;

namespace ExamProcessManage.Dtos
{
    public class ExamSetDTO
    {
        public ExamSetDTO()
        {
            exams = new HashSet<ExamDTO>();
        }

        public int? id { get; set; }
        public string? name { get; set; }
        public CommonObject? department { get; set; }
        public CommonObject? major { get; set; }
        public int? exam_quantity { get; set; }
        public string? description { get; set; }
        public string status { get; set; }
        public Boolean? isGetForAddProposal { get; set; } = false;
        public CommonObject course { get; set; }
        public CommonObject? proposal { get; set; }
        public virtual object? user { get; set; }
        public string? create_at { get; set; }
        public string? update_at { get; set; }
        public IEnumerable<ExamDTO>? exams { get; set; }
    }
}
