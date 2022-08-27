using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CarrotHome.Mqtt.Carrot;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace CarrotHome.Mqtt;

public class CarrotAdaptor
	: IConnectableCache<LightStatus, int>
	, IDisposable
{
	private readonly IObservable<IConnectableCache<LightStatus, int>> _cache;


	public CarrotAdaptor(
		ICarrotService carrotService,
		ILogger<CarrotAdaptor> logger)
	{
		_cache = Observable.Create<IConnectableCache<LightStatus, int>>(async (observable, stoppingToken) =>
			{
				using var cache = new SourceCache<LightStatus, int>(x => x.DeviceId);
				observable.OnNext(cache);

				using var logonTask = EnsureLoggedIn(carrotService);

				while (!stoppingToken.IsCancellationRequested)
				{
					try
					{
						await RunCache(cache, carrotService, stoppingToken);
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

	public void Dispose()
	{
		
	}

	private static IDisposable EnsureLoggedIn(ICarrotService carrotService)
	{
		return TaskPoolScheduler.Default.ScheduleAsync(async (scheduler, stoppingToken) =>
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				_ = await carrotService.LoginAsync(
					Environment.GetEnvironmentVariable("CARROT_USER")!, 
					Environment.GetEnvironmentVariable("CARROT_PASS")!, 
					stoppingToken);

				await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
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
}