using System.Threading;
using System.Threading.Tasks;
using RestEase;

namespace CarrotHome.Mqtt.Carrot;

public interface ICarrotService
{
	[Get("cloud/servlet/LoginServlet?action=login&apptype=androidfree&appversion=14")]
	Task<LoginResponse> LoginAsync(
		[Query] string email,
		[Query("key")] string password,
		CancellationToken cancellationToken = default
	);

	[Get("cloud/servlet/CarrotControlServlet?action=getlightstatus")]
	Task<LightStatusResponse> GetLightStatus(CancellationToken cancellationToken = default);

	[Get("cloud/servlet/CarrotControlServlet?action=control")]
	Task SetLight(
		[Query("deviceid")] int deviceId, 
		[Query] int value, 
		CancellationToken cancellationToken = default);
}