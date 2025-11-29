using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using FocusDeck.Shared.Contracts.Auth;
using FocusDeck.Shared.Security;

var baseUrl = Environment.GetEnvironmentVariable("FOCUSDECK_BASE_URL") ?? "http://localhost:5000/";
if (!baseUrl.EndsWith('/'))
{
	baseUrl += "/";
}

var password = Environment.GetEnvironmentVariable("FOCUSDECK_TEST_PASSWORD") ?? "TestPassword123!";
var devicePlatform = Environment.GetEnvironmentVariable("FOCUSDECK_DEVICE_PLATFORM") ?? "web";
var userId = args.Length > 0 ? args[0] : $"ci-bot+{DateTime.UtcNow:yyyyMMddHHmmss}@focusdeck.study";
var normalizedUserId = userId.Trim().ToLowerInvariant();
var clientId = $"cli-{Guid.NewGuid():N}";
var deviceName = "CI Smoke Test";

var http = new HttpClient
{
	BaseAddress = new Uri(baseUrl),
	Timeout = TimeSpan.FromSeconds(30)
};

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
	PropertyNameCaseInsensitive = true
};

try
{
	Console.WriteLine($"[+] Register start for {userId}");
	var registerStart = await PostAsync<RegisterStartResponse>(http, "v1/auth/pake/register/start", new RegisterStartRequest(userId, devicePlatform), jsonOptions);

	var registerKdf = ParseKdf(registerStart.KdfParametersJson, jsonOptions);
	var registerPrivateKey = ComputePrivateKey(registerKdf, normalizedUserId, password);
	var verifier = Srp.ComputeVerifier(registerPrivateKey);
	var verifierBase64 = Convert.ToBase64String(Srp.ToBigEndian(verifier));

	Console.WriteLine("[+] Register finish");
	var registerFinish = await PostAsync<RegisterFinishResponse>(http,
		"v1/auth/pake/register/finish",
		new RegisterFinishRequest(userId, verifierBase64, registerStart.KdfParametersJson, null, null, null),
		jsonOptions);

	if (!registerFinish.Success)
	{
		throw new InvalidOperationException("Register finish reported failure");
	}

	Console.WriteLine("[+] Login start");
	var (clientSecret, clientPublic) = Srp.GenerateClientEphemeral();
	var loginStart = await PostAsync<LoginStartResponse>(http,
		"v1/auth/pake/login/start",
		new LoginStartRequest(userId, Convert.ToBase64String(Srp.ToBigEndian(clientPublic)), clientId, deviceName, devicePlatform),
		jsonOptions);

	var loginKdf = ParseKdf(loginStart.KdfParametersJson, jsonOptions, loginStart.SaltBase64);
	var privateKey = ComputePrivateKey(loginKdf, normalizedUserId, password);
	var serverPublic = Srp.FromBigEndian(Convert.FromBase64String(loginStart.ServerPublicEphemeralBase64));
	var scramble = Srp.ComputeScramble(clientPublic, serverPublic);
	if (scramble.Sign == 0)
	{
		throw new InvalidOperationException("Server returned zero scramble");
	}

	var session = Srp.ComputeClientSession(serverPublic, privateKey, clientSecret, scramble);
	var sessionKey = Srp.ComputeSessionKey(session);
	var clientProof = Srp.ComputeClientProof(clientPublic, serverPublic, sessionKey);

	Console.WriteLine("[+] Login finish");
	var loginFinish = await PostAsync<LoginFinishResponse>(http,
		"v1/auth/pake/login/finish",
		new LoginFinishRequest(userId, loginStart.SessionId, Convert.ToBase64String(clientProof), clientId, deviceName, devicePlatform),
		jsonOptions);

	var expectedServerProof = Srp.ComputeServerProof(clientPublic, clientProof, sessionKey);
	var actualServerProof = Convert.FromBase64String(loginFinish.ServerProofBase64);
	if (!actualServerProof.SequenceEqual(expectedServerProof))
	{
		throw new InvalidOperationException("Server proof validation failed");
	}

	Console.WriteLine($"[✓] Login succeeded. Tenant token expires in {loginFinish.ExpiresIn} seconds");
}
catch (HttpRequestException httpEx)
{
	Console.Error.WriteLine($"HTTP error: {httpEx.Message}");
	if (httpEx.StatusCode is not null)
	{
		Console.Error.WriteLine($"Status code: {(int)httpEx.StatusCode} ({httpEx.StatusCode})");
	}
	return 1;
}
catch (Exception ex)
{
	Console.Error.WriteLine($"Failure: {ex.Message}");
	Console.Error.WriteLine(ex);
	return 1;
}

return 0;

static async Task<T> PostAsync<T>(HttpClient http, string path, object payload, JsonSerializerOptions options, CancellationToken cancellationToken = default)
{
	using var response = await http.PostAsJsonAsync(path, payload, cancellationToken: cancellationToken);
	var content = await response.Content.ReadAsStringAsync(cancellationToken);
	if (!response.IsSuccessStatusCode)
	{
		throw new HttpRequestException($"Request to {path} failed: {(int)response.StatusCode} {response.StatusCode} - {content}", inner: null, response.StatusCode);
	}

	var result = JsonSerializer.Deserialize<T>(content, options);
	if (result == null)
	{
		throw new InvalidOperationException($"Failed to deserialize response from {path}. Raw content: {content}");
	}
	return result;
}

static SrpKdfParameters ParseKdf(string? json, JsonSerializerOptions options, string? fallbackSalt = null)
{
	if (!string.IsNullOrWhiteSpace(json))
	{
		var parsed = JsonSerializer.Deserialize<SrpKdfParameters>(json, options);
		if (parsed != null)
		{
			return parsed;
		}
	}

	if (string.IsNullOrWhiteSpace(fallbackSalt))
	{
		throw new InvalidOperationException("KDF parameters missing and no fallback salt provided");
	}

	return new SrpKdfParameters("sha256", fallbackSalt, aad: false);
}

static BigInteger ComputePrivateKey(SrpKdfParameters kdf, string userId, string password)
{
	if (string.Equals(kdf.Algorithm, "sha256", StringComparison.OrdinalIgnoreCase))
	{
		return Srp.ComputePrivateKey(Convert.FromBase64String(kdf.SaltBase64), userId, password);
	}

	return Srp.ComputePrivateKey(kdf, userId, password);
}
