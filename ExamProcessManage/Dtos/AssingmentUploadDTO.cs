namespace ExamProcessManage.Dtos
{
    public class AssingmentUploadDTO
    {
        public ulong id { get; set; }
        public ulong assignment_proposal_id { get; set; }
        public DateOnly upload_date { get; set; }
        public string file_path { get; set; }
        public string status { get; set; }
        public DateTime? create_at { get; set; }
        public DateTime? update_at { get;}
    }
}
