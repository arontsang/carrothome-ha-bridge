using System;
using System.Globalization;
using Newtonsoft.Json;

namespace CarrotHome.Mqtt.Carrot;

public class CarrotLightValueConverter
	: JsonConverter<LightState>
{
	public override void WriteJson(JsonWriter writer, LightState value, JsonSerializer serializer)
	{
		switch (value)
		{
			case LightState.ON:
				writer.WriteValue("64");
				return;
			default:
				writer.WriteValue("0");
				return;
		}
		
	}

	public override LightState ReadJson(JsonReader reader, Type objectType, LightState existingValue, bool hasExistingValue,
		JsonSerializer serializer)
	{
		if (reader.Value is string hex &&
		    int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
		{
			switch (value)
			{
				case 0:
					return LightState.OFF;
				default:
					return LightState.ON;
			}
			
			
		}

		//if (int.TryParse(value, out var x) && x == 0)
		//	return LightState.OFF;
		return LightState.ON;
	}
}