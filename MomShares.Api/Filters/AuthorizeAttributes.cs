using Microsoft.AspNetCore.Authorization;

namespace MomShares.Api.Filters;

/// <summary>
/// 管理员权限要求
/// </summary>
public class AdminAuthorizeAttribute : AuthorizeAttribute
{
    public AdminAuthorizeAttribute()
    {
        Policy = "AdminOnly";
    }
}

/// <summary>
/// 持有者权限要求
/// </summary>
public class HolderAuthorizeAttribute : AuthorizeAttribute
{
    public HolderAuthorizeAttribute()
    {
        Policy = "HolderOnly";
    }
}

