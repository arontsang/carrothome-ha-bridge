namespace CarrotHome.Mqtt.Carrot;

public class LoginResponse
{
	public OperationResult Result { get; init; }
}

public enum OperationResult
{
	// ReSharper disable once InconsistentNaming
	success,
	// ReSharper disable once InconsistentNaming
	fail,
}