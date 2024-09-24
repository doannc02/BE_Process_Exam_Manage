namespace ExamProcessManage.ResponseModels
{
    public class AssignmentUploadResponse
    {
        public ulong id {  get; set; }
        public ulong proposal_id { get; set; }
        public string file_path { get; set; }
        public string status { get; set; }
        public string upload_date { get; set; }

    }
}
