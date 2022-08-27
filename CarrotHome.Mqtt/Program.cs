// See https://aka.ms/new-console-template for more information

using CarrotHome.Mqtt;
using CarrotHome.Mqtt.Carrot;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestEase.HttpClientFactory;

Console.WriteLine("Hello, World!");

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHttpClient("Carrot")
			.ConfigureInsecureHttps()
			.ConfigureHttpClient(x => x.BaseAddress = new Uri(Environment.GetEnvironmentVariable("CARROT_HOST")!))
			.UseWithRestEaseClient<ICarrotService>();
		
		services.AddSingleton<MqttAdaptorService>();
		services.AddSingleton<CarrotAdaptor>();

		services.AddTransient<IConnectableCache<LightStatus, int>, CarrotAdaptor>();
		services.AddTransient<IHostedService, MqttAdaptorService>();
	})
	.UseSystemd()
	.Build();

host.Run();
