using Microsoft.Extensions.DependencyInjection;

namespace CarrotHome.Mqtt;

public static class HttpsHelper
{
	public static IHttpClientBuilder ConfigureInsecureHttps(this IHttpClientBuilder builder)
	{
		if (Environment.GetEnvironmentVariable("DISABLE_SECURE_HTTPS") is { } setting
		    && Boolean.TryParse(setting, out var value)
		    && value == true)
		{
			return builder.ConfigurePrimaryHttpMessageHandler(() =>
			{
				return new HttpClientHandler()
				{
					ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
				};
			});
		}

		return builder;
	}
}