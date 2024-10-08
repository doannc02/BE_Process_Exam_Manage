﻿using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

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
        public string department { get; set; } = null!;
        public string major { get; set; } = null!;
        public int total_exams { get; set; }
        public int exam_quantity { get; set; }
        public string? description { get; set; }
        public string status { get; set; } = null!;
        public  CommonObject course { get; set; }
        public  CommonObject? proposal { get; set; }
        public virtual CommonObject? user { get; set; }
        public  IEnumerable<ExamDTO> exams { get; set; }
    }
}
