namespace PDPWebsite;

public class SpaProxyFixes
{
    public static HttpClient CreateHttpClientForProxy(TimeSpan requestTimeout)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpClient(handler)
        {
            Timeout = requestTimeout
        };
    }
}
