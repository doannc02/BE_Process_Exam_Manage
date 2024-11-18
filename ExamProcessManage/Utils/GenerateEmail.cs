using System.Text;

namespace ExamProcessManage.Utils
{
    public static class GenerateEmail
    {
        public static string GenerateEmailBody(bool isApproved, string proposalTitle, string proposalDescription, int proposalId,
            string submissionDate, string adminName, string userName)
        {
            var sb = new StringBuilder();

            // Header and Style
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"vi\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("<title>Thông Báo Phê Duyệt</title>");
            sb.AppendLine("</head>");
            sb.AppendLine(
                "<body style=\"font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f6f6f6;\">");

            sb.AppendLine(
                "<div style=\"width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);\">");

            // Header
            sb.AppendLine("<div style=\"background-color: " + (isApproved ? "#16A34A" : "#D32F2F") +
                          "; color: white; padding: 10px 0; text-align: center; border-radius: 8px 8px 0 0;\">");
            sb.AppendLine("<img style=\"width: 80px; border-radius: 50%; margin-right: 10px; vertical-align: middle; display: inline-block;\" src=\"http://itf.viu.edu.vn/build/assets/logodhcn1-16af8a30.jpg\" alt=\"viu-itf-logo\">");
            sb.AppendLine("<h1 style=\"margin: 0; display: inline-block; vertical-align: middle;\">VIU - Exam Process Manage</h1>");
            sb.AppendLine("</div>");

            // Body Content
            sb.AppendLine("<div style=\"padding: 20px; text-align: left;\">");
            sb.AppendLine("<p>Kính gửi " + userName + ",</p>");

            sb.AppendLine(isApproved
                ? "<p>Chúng tôi rất vui mừng thông báo rằng đề xuất của bạn đã được quản trị viên phê duyệt.</p>"
                : "<p>Chúng tôi rất tiếc phải thông báo rằng đề xuất của bạn đã bị quản trị viên từ chối.</p>");

            sb.AppendLine("<p>Chi tiết đề xuất:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li><strong>Mã:</strong> " + proposalTitle + "</li>");
            sb.AppendLine("<li><strong>Mô tả:</strong> " + proposalDescription + "</li>");
            sb.AppendLine("<li><strong>Ngày:</strong> " + submissionDate + "</li>");
            sb.AppendLine("<li><strong>Người phê duyệt:</strong> " + adminName + "</li>");
            sb.AppendLine("</ul>");

            // Add button for navigation
            sb.AppendLine("<p><a href=\"https://itf.viu.edu.vn:880/qldethi/proposal/" + proposalId + "\" style=\"display: inline-block; padding: 10px 20px; background-color:#16a34a; color: white; text-decoration: none; border-radius: 5px; font-size: 16px;\">Xem chi tiết đề xuất</a></p>");

            if (isApproved)
            {
                sb.AppendLine(
                    "<p>Các thông tin liên quan đến kế hoạch sau khi được phê duyệt sẽ tự động được lưu lại trong hệ thống.</p>");
                sb.AppendLine(
                    "<p>Hãy đảm bảo rằng tất cả các thông tin về kế hoạch này là đầy đủ và chính xác. Nếu có bất kỳ thay đổi nào trong kế hoạch của bạn, vui lòng thông báo cho chúng tôi để chúng tôi có thể hỗ trợ bạn kịp thời.</p>");
                sb.AppendLine(
                    "<p>Trân trọng!</p>");
            }
            else
            {
                sb.AppendLine(
                    "<p>Chúng tôi rất tiếc về quyết định này, nhưng bạn có thể điều chỉnh lại đề xuất của mình và gửi lại để được xem xét thêm. Vui lòng kiểm tra các điểm chưa phù hợp hoặc cần cải thiện trong đề xuất của bạn.</p>");
                sb.AppendLine(
                    "<p>Chúng tôi khuyến khích bạn cập nhật lại và nộp lại trong thời gian sớm nhất. Nếu cần thêm sự hỗ trợ hoặc có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>");
                sb.AppendLine("<p>Trân trọng!</p>");
            }

            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine(
                "<div style=\"background-color: #f4f4f4; color: #555555; text-align: center; padding: 10px 0; font-size: 12px;\">");
            sb.AppendLine(
                "<p>© 2024 Khoa Công nghệ Thông tin. Trường Đại học Công nghiệp Việt - Hung. Tất cả quyền được bảo lưu.</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
