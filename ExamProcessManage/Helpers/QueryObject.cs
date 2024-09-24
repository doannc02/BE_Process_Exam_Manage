namespace ExamProcessManage.Helpers
{
    public class QueryObject
    {
        public int? userId { get; set; }
        public string? sort { get; set; } = null;
        public int? page { get; set; } = 1;
        public int size { get; set; } = 20;
        public string search { get; set; } = "";
        public int? exceptId { get; set; }
        public int? roleId { get; set; }    
    }

    public class QueryObjectProposal : QueryObject
    {
         public string? startDate { get; set; }      
         public string? endDate { get; set; }
         public  int? semester { get; set; }
         public int? status { get; set; }
    }

    public class QueryObjectCourse : QueryObject
    {
    }
}
