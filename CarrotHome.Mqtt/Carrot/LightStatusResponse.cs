using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CarrotHome.Mqtt.Carrot;

public class LightStatusResponse
{

	public OperationResult Result { get; [UsedImplicitly] init; } = OperationResult.fail;
	public LightStatus[] Devices { get; [UsedImplicitly] init; } = Array.Empty<LightStatus>();
}

public class LightStatus
{

	[JsonConverter(typeof(CarrotLightValueConverter))]
	public LightState Value { get; init; }
	[JsonProperty("deviceid")]
	public int DeviceId { get; init; }


	protected bool Equals(LightStatus other)
	{
		return Value == other.Value && DeviceId == other.DeviceId;
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((LightStatus)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine((int)Value, DeviceId);
	}
}


public enum LightState
{
	// ReSharper disable once InconsistentNaming
	OFF = 0,
	// ReSharper disable once InconsistentNaming
	ON = 64,
}