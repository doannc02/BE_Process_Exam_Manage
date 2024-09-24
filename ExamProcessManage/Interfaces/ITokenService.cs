
using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

namespace ExamProcessManage.Interfaces

{
    public interface ITokenService
    {

        TokenResponse GenerateToken(User user);
        int? ValidateToken(string token);
        string CreateToken(User user);
    }
}

