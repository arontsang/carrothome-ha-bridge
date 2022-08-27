using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarrotHome.Mqtt.Carrot;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestEase;

namespace CarrotHome.Mqtt;

public class CarrotAdaptor
	: IConnectableCache<LightStatus, int>
{
	private readonly IOptions<CarrotAdaptorOptions> _options;
	private readonly IObservable<IConnectableCache<LightStatus, int>> _cache;
	private readonly ICarrotService _carrotService;

	public CarrotAdaptor(
		IHttpClientFactory clientFactory,
		IOptions<CarrotAdaptorOptions> options,
		ILogger<CarrotAdaptor> logger)
	{
		var foo = clientFactory.CreateClient();
		foo.BaseAddress = new(options.Value.Server);
		
		
		_carrotService = new RestClient(foo)
			.For<ICarrotService>();
		
		_options = options;
		_cache = Observable.Create<IConnectableCache<LightStatus, int>>(async (observable, stoppingToken) =>
			{
				using var cache = new SourceCache<LightStatus, int>(x => x.DeviceId);
				observable.OnNext(cache);

				using var logonTask = EnsureLoggedIn(_carrotService);

				while (!stoppingToken.IsCancellationRequested)
				{
					try
					{
						await RunCache(cache, _carrotService, stoppingToken);
					}
					catch (OperationCanceledException)
					{
						
					}
					catch (Exception ex)
					{
						logger.LogError(ex, $"Unexpected error in {nameof(CarrotAdaptor)}");
					}
				}
				
			})
			.Replay(1)
			.RefCount();
	}

	public IObservable<IChangeSet<LightStatus, int>> Connect(Func<LightStatus, bool>? predicate = null, bool suppressEmptyChangeSets = true)
	{
		return _cache.Select(x => x.Connect(predicate))
			.Switch();
	}

	public IObservable<IChangeSet<LightStatus, int>> Preview(Func<LightStatus, bool>? predicate = null)
	{
		return _cache.Select(x => x.Preview(predicate))
			.Switch();
	}

	public IObservable<Change<LightStatus, int>> Watch(int key)
	{
		return _cache.Select(x => x.Watch(key))
			.Switch();
	}

	public IObservable<int> CountChanged => _cache
		.Select(x => x.CountChanged)
		.Switch();

	private IDisposable EnsureLoggedIn(ICarrotService carrotService)
	{
		return TaskPoolScheduler.Default.ScheduleAsync(async (scheduler, stoppingToken) =>
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await carrotService.LoginAsync(
					_options.Value.User, 
					_options.Value.Pass, 
					stoppingToken);

				await scheduler.Sleep(TimeSpan.FromHours(3), stoppingToken);
			}
		});
	}

	private static async Task RunCache(
		ISourceCache<LightStatus, int> cache, 
		ICarrotService carrotService,
		CancellationToken stoppingToken)
	{
		await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			var status = await carrotService.GetLightStatus(stoppingToken);
			if (status.Result == OperationResult.success)
				cache.EditDiff(status.Devices, EqualityComparer<LightStatus>.Default);

			await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
		}
	}

	public Task SetLight(
		int deviceId,
		LightState state,
		CancellationToken cancellationToken = default)
	{
		return _carrotService.SetLight(deviceId, (int)state);
	}
}

public class CarrotAdaptorOptions
{
	public string Server { get; [UsedImplicitly] set; } = null!;
	public string User { get; [UsedImplicitly] set; } = null!;
	public string Pass { get; [UsedImplicitly] set; } = null!;
}