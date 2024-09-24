namespace ExamProcessManage.Helpers
{
    public class AuthenticateResponse
    {
        public string accessToken {  get; set; }
        public string tokenType { get; set; }
        public string expiresIn { get; set; }
        public string scopes { get; set; }
        public ulong userId { get; set; }
        public string jti { get; set; }
    }
}
