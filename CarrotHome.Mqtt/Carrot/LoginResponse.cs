namespace CarrotHome.Mqtt.Carrot;

public class LoginResponse
{
	public OperationResult Result { get; init; }
}

public enum OperationResult
{
	success,
	fail,
}