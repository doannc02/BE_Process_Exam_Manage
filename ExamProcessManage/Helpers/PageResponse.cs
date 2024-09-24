namespace ExamProcessManage.Helpers
{
    public class PageResponse<T>
    {
        public IEnumerable<T> content { get; set; }  // Dữ liệu trang hiện tại
        public int page { get; set; }  // Số trang hiện tại
        public int size { get; set; }  // Kích thước trang
        public string sort { get; set; }  // Sắp xếp theo tiêu chí nào
        public int totalElements { get; set; }  // Tổng số phần tử
        public int totalPages { get; set; }  // Tổng số trang
        public int numberOfElements { get; set; }  // Số phần tử trên trang hiện tại
    }

    public class CommonResponse<T>
    {
        public string message { get; set; }  // Thông điệp trả về, ví dụ: "Thành công"
        public string traceId { get; set; }  // TraceId để theo dõi các request
        public T data { get; set; }  // Dữ liệu trả về, dạng generic
    }
}
