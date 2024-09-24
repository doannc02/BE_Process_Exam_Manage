namespace ExamProcessManage.Services
{
    public class FMPService 

    {
       private HttpClient _httpClient;
       private IConfiguration _configuration;
       public FMPService(HttpClient httpClient, IConfiguration configuration) {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        
    }
}
