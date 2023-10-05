using Microsoft.AspNetCore.Mvc.Filters;

namespace PDPWebsite.Middlewares;

public class AuthFilter : IAsyncActionFilter
{
    private readonly RedisClient _rClient;

    public AuthFilter(RedisClient rClient)
    {
        _rClient = rClient;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == "OPTIONS")
        {
            await next();
            return;
        }
        var token = context.HttpContext.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _rClient.GetObj<Login>(token);
        if (loginRecord is null && context.HttpContext.Request.Path.Value!.Contains("api") && !context.HttpContext.Request.Path.Value!.Contains("login"))
        {
            context.HttpContext.Response.StatusCode = 401;
            context.HttpContext.Response.ContentType = "text/plain";
            await context.HttpContext.Response.WriteAsync("Unauthorized");
            return;
        }
        await next();
    }
}
