using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CarrotHome.Mqtt.Carrot;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CarrotHome.Mqtt;

public class MqttAdaptorService
	: BackgroundService
	
{
	private readonly IConnectableCache<LightStatus, int> _lightCache;
	private readonly IOptions<MqttAdaptorOptions> _options;
	private readonly CarrotAdaptor _carrotService;
	private readonly IManagedMqttClient _client;
	private readonly JsonSerializer _serializer;

	public MqttAdaptorService(
		IConnectableCache<LightStatus, int> lightCache, 
		IOptions<MqttAdaptorOptions> options,
		CarrotAdaptor carrotService)
	{
		_lightCache = lightCache;
		_options = options;
		_carrotService = carrotService;
		
		_serializer = new JsonSerializer();
		_serializer.Converters.Add(new StringEnumConverter());
		var factory = new MqttFactory();
		_client = factory.CreateManagedMqttClient();
	}

	public override void Dispose()
	{
		base.Dispose();
		_client.Dispose();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var port = _options.Value.Port switch
		{
			{ } value => value,
			null when _options.Value!.UseTls => 8883,
			_ => 1883
		};
		
		var clientOptionsBuilder = new MqttClientOptionsBuilder()
			.WithTcpServer(
				_options.Value.Server, 
				port)
			.WithCredentials(
				_options.Value.User,
				_options.Value.Pass);

		if (_options.Value.UseTls)
			clientOptionsBuilder = clientOptionsBuilder.WithTls();
		
		var clientOptions = clientOptionsBuilder
			.Build();
		
		var options = new ManagedMqttClientOptionsBuilder()
			.WithClientOptions(clientOptions)
			.Build();
		await _client.StartAsync(options);


		await _client.SubscribeAsync("homeassistant/light/+/set");
		await _client.SubscribeAsync("homeassistant/status");
		_client.ApplicationMessageReceivedAsync += OnLightStateSet;
		
		

		await foreach (var updates in _lightCache.Connect().ToAsyncEnumerable().WithCancellation(stoppingToken))
		{
			foreach (var update in updates)
			{
				var name = $"carrothome{update.Key}";
				var discoveryTopic = $"homeassistant/light/{name}/config";

				switch (update.Reason)
				{
					case ChangeReason.Add:
					{
						await DescribeLight(update.Current);
						break;
					}

					case ChangeReason.Remove:
					{
						await _client.EnqueueAsync(discoveryTopic, "");
						continue;
					}
				}

				await UpdateState(update.Key, update.Current.Value);
			}
		}

	}


	private async Task UpdateState(int deviceId, LightState state)
	{
		var name = $"carrothome{deviceId}";
		var baseTopic = $"homeassistant/light/{name}";
		var statePayload = new StatePayload(state);
		await using var stringBuilder = new StringWriter();
		_serializer.Serialize(stringBuilder, statePayload);
		var json = stringBuilder.ToString();
		await _client.EnqueueAsync($"{baseTopic}/state", json);
	}

	private static readonly Regex Regex = new Regex("^homeassistant/light/carrothome(?<id>[0-9]*)/set$", RegexOptions.Compiled);
	
	async Task OnLightStateSet(MqttApplicationMessageReceivedEventArgs args)
	{
		args.AutoAcknowledge = false;
		if (Regex.Match(args.ApplicationMessage.Topic) is { Success: true } topicMatch && topicMatch.Groups["id"] is { Value: { } idStr } && int.TryParse(idStr, out var id))
		{
			using var stream = new MemoryStream(args.ApplicationMessage.Payload);
			using var json = new JsonTextReader(new StreamReader(stream));
			var payload = _serializer.Deserialize<StatePayload>(json);
			
			await _carrotService.SetLight(id, payload.State);
			await using var stringBuilder = new StringWriter();
			_serializer.Serialize(stringBuilder, payload);
			var updatedState = stringBuilder.ToString();

			await _client.EnqueueAsync($"carrot/light/carrothome{id}/state", updatedState);
			
			await args.AcknowledgeAsync(default);
			return;
		}

		if (args.ApplicationMessage.Topic == "homeassistant/status")
		{
			var snapshot = await _lightCache.Connect().ToCollection()
				.ToAsyncEnumerable()
				.FirstAsync();

			foreach (var light in snapshot)
			{
				await DescribeLight(light);
				await UpdateState(light.DeviceId, light.Value);
			}
			await args.AcknowledgeAsync(default);
			return;
		}
	}
	
	private async Task DescribeLight(LightStatus light)
	{
		var name = $"carrothome{light.DeviceId}";
		var baseTopic = $"homeassistant/light/{name}";
		var discoveryTopic = $"homeassistant/light/{name}/config";
		
		var payload = new DiscoveryPayload(name, baseTopic);

		await using var stringBuilder = new StringWriter();
		_serializer.Serialize(stringBuilder, payload);
		var json = stringBuilder.ToString();
		await _client.EnqueueAsync(discoveryTopic, json);
	}

	private class DiscoveryPayload
	{
		public DiscoveryPayload(string name, string baseTopic)
		{
			Name = name;
			BaseTopic = baseTopic;
		}

		[JsonProperty("name")] private string Name { get; init; }
		[JsonProperty("unique_id")] private string UniqueId => Name;
		[JsonProperty("~")] string BaseTopic { get; init; }
		
		[JsonProperty("stat_t")] public string StateTopic => "~/state";

		[JsonProperty("cmd_t")] public string CommandTopic => "~/set";
		
		[JsonProperty("schema")] public string Schema => "json";
	}

	private class StatePayload
	{
		public StatePayload(LightState state)
		{
			State = state;
		}

		[JsonProperty("state")] 
		[JsonConverter(typeof(StringEnumConverter))]  
		public LightState State { get; }
	} 
}

public class MqttAdaptorOptions
{
	public string Server { get; [UsedImplicitly] set; } = null!;
	public int? Port { get; [UsedImplicitly] set; }
	public bool UseTls { get; [UsedImplicitly] set; } = false;
	public string User { get; [UsedImplicitly] set; } = null!;
	public string Pass { get; [UsedImplicitly] set; } = null!;
}