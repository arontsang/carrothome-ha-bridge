using System.Net.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarrotHome.Mqtt;

public static class HttpsHelper
{
	private static bool ShouldValidateHttps(this IServiceProvider services)
	{
		if (services.GetService<IConfiguration>() is { } config
		    && config["Http:Validate_Https"] is { } validateHttpsStr
		    && Boolean.TryParse(validateHttpsStr, out var validateHttps))
			return validateHttps;
		return true;
	}
	
	public static IHttpClientBuilder ConfigureInsecureHttps(this IHttpClientBuilder builder)
	{

		builder.Services.AddScoped<SocketsHttpHandler>();
		builder.ConfigurePrimaryHttpMessageHandler<SocketsHttpHandler>();
		
		builder.ConfigurePrimaryHttpMessageHandler((handler, serviceProvider) =>
		{
			var logger = serviceProvider.GetService<ILogger<HttpClientHandler>>();
			if (serviceProvider.ShouldValidateHttps()
			    || handler is not SocketsHttpHandler socketsHttpHandler) return;
			logger?.LogWarning("Disable SSL verification");
			if (OperatingSystem.IsLinux())
			{
				socketsHttpHandler.SslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
				{
					TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
					TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
					TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
				});
			}
			socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
		});

		return builder;
	}
}