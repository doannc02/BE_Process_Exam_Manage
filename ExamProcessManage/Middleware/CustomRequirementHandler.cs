using Microsoft.AspNetCore.Authorization;

namespace ExamProcessManage.Middleware
{
    public class CustomRequirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public CustomRequirement(string requiredRole)
        {
            RequiredRole = requiredRole;
        }
    }
    public class CustomRequirementHandler : AuthorizationHandler<CustomRequirement>
    {
        protected override Task HandleRequirementAsync(
       AuthorizationHandlerContext context,
       CustomRequirement requirement)
        {
            // Đặt breakpoint tại đây để kiểm tra logic quyền
          
                context.Succeed(requirement);
            

            return Task.CompletedTask;
        }
    }
}
