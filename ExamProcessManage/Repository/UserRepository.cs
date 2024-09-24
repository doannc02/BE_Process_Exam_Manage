using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;

namespace ExamProcessManage.Repository
{
    public class UserRepository : IUserRepository
    {
        public Task<BaseResponse<UserDTO>> GetDetailUserAsync(int userID)
        {
            throw new NotImplementedException();
        }

        public Task<PageResponse<UserDTO>> GetListUsersAsync(QueryObject query)
        {
            throw new NotImplementedException();
        }
    }
}
