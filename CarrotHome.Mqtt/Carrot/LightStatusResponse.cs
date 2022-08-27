using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CarrotHome.Mqtt.Carrot;

public class LightStatusResponse
{

	public OperationResult Result { get; [UsedImplicitly] init; } = OperationResult.fail;
	public LightStatus[] Devices { get; [UsedImplicitly] init; } = Array.Empty<LightStatus>();
}

public record LightStatus(LightState Value, [JsonProperty("deviceid")] int DeviceId);

public enum LightState
{
	OFF = 0,
	ON = 64,
}