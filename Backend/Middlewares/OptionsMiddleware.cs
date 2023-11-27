namespace PDPWebsite.Middlewares;

public class OptionsMiddleware
{
    private readonly RequestDelegate _next;

    public OptionsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("OK");
            return;
        }

        await _next.Invoke(context);
    }
}