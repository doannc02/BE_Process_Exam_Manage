
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces
{
    public interface IUserService
    {
        AuthenticateResponse Authenticate(LoginDto model);
       // IEnumerable<User> GetAll();
        //void Register(RegisterRequest model);
       // void Update(int id, UpdateRequest model);
       // void Delete(int id);
    }
}
