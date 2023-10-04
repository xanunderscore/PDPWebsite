namespace PDPWebsite.Middlewares;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisClient _rClient;

    public AuthMiddleware(RequestDelegate next, RedisClient rClient)
    {
        _next = next;
        _rClient = rClient;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            await _next.Invoke(context);
            return;
        }
        var token = context.Request.Headers["Authorization"].ToString().Split(" ").Last();
        var loginRecord = _rClient.GetObj<Login>(token);
        if (loginRecord is null && context.Request.Path.Value!.Contains("api") && !context.Request.Path.Value!.Contains("login"))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        await _next.Invoke(context);
    }
}
