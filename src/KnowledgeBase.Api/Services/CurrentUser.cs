using KnowledgeBase.Core.Interfaces;
using Microsoft.Identity.Web;

namespace KnowledgeBase.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("Authenticated user principal is required.");
            }

            var objectIdClaim = user.GetObjectId();
            if (string.IsNullOrWhiteSpace(objectIdClaim) || !Guid.TryParse(objectIdClaim, out var userId))
            {
                throw new InvalidOperationException("Authenticated user is missing a valid Microsoft Entra object ID ('oid') claim.");
            }

            return userId;
        }
    }
}
