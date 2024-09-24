using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;

namespace ExamProcessManage.Services
{
    public class AccountService : IUserService
    {
        private readonly ITokenService _jwtUtils;
         private readonly ApplicationDbContext _context;

        public AccountService(ITokenService jwtUtils, ApplicationDbContext context)
        {
            _jwtUtils = jwtUtils;
            _context = context;
            _context = context;
        }

        public AuthenticateResponse Authenticate(LoginDto model)
        {
            var user = _context.Users.SingleOrDefault(x => x.Email == model.UserName);
          if (user == null)
        {
                // throw new ApplicationException("Username or password is incorrect");
                return null;
            }

            if (!VerifyPassword.VerifyPasswordBCrypt(model.Password, user.Password))
            {
                return new AuthenticateResponse
                {
                    accessToken = null,
                    tokenType = "Bearer",
                    expiresIn = "3600", // Thời gian hết hạn tùy vào cấu hình
                    scopes = "read write",
                    userId = user.Id,
                    jti = Guid.NewGuid().ToString()
                };
            }

           //  var token = _jwtUtils.GenerateToken(user);
            var token = _jwtUtils.CreateToken(user);

            return new AuthenticateResponse
            {
                accessToken = token,
                tokenType = "Bearer",
                expiresIn = "3600", // Thời gian hết hạn tùy vào cấu hình
                scopes = "read write",
                userId = user.Id,
                jti = Guid.NewGuid().ToString()
            };
        }


    }
}
