using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
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
		
		builder.ConfigureHttpMessageHandlerBuilder(handlerBuilder =>
		{
			var logger = handlerBuilder.Services.GetService<ILogger<HttpClientHandler>>();
			if (handlerBuilder.Services.ShouldValidateHttps() == false)
			{
				if (handlerBuilder.PrimaryHandler is SocketsHttpHandler socketsHttpHandler)
				{
					logger?.LogWarning("Disable SSL verification");
					socketsHttpHandler.SslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
					{
						TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
						TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
						TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
					});
					socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
				}
			}
		});

		return builder;
	}
}