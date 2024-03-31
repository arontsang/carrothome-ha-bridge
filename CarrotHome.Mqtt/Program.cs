// See https://aka.ms/new-console-template for more information

using System.Net;
using CarrotHome.Mqtt;
using CarrotHome.Mqtt.Carrot;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase.HttpClientFactory;


ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddOptions<CarrotAdaptorOptions>()
			.BindConfiguration("Carrot");
		services.AddOptions<MqttAdaptorOptions>()
			.BindConfiguration("Mqtt");
		
		services.AddHttpClient(string.Empty)
			.ConfigureInsecureHttps()
			.UseWithRestEaseClient<ICarrotService>();
		
		services.AddSingleton<MqttAdaptorService>();
		services.AddSingleton<CarrotAdaptor>();

		services.AddSingleton<IConnectableCache<LightStatus, int>>(provider => provider.GetRequiredService<CarrotAdaptor>());
		services.AddTransient<IHostedService>(provider => provider.GetRequiredService<MqttAdaptorService>());
	})
	.UseSystemd()
	.Build();

host.Run();

