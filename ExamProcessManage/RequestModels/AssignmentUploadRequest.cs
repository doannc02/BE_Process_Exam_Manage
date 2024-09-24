using ExamProcessManage.Helpers;

namespace ExamProcessManage.RequestModels
{
    public class AssignmentUploadRequest : QueryObject
    {
        public int id {  get; set; }
        public int id_proposal { get; set; }
    }
}
