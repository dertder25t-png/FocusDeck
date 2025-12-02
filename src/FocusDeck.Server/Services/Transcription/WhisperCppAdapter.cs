using System.Diagnostics;

namespace FocusDeck.Server.Services.Transcription;

/// <summary>
/// Adapter for Whisper.cpp command-line transcription
/// </summary>
public class WhisperCppAdapter : IWhisperAdapter
{
    private readonly string _whisperExecutablePath;
    private readonly ILogger<WhisperCppAdapter> _logger;

    public WhisperCppAdapter(IConfiguration configuration, ILogger<WhisperCppAdapter> logger)
    {
        _whisperExecutablePath = configuration["Whisper:ExecutablePath"] ?? "/usr/local/bin/whisper-cpp";
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, string language = "en", CancellationToken cancellationToken = default)
    {
        // 1. Validate Executable Existence
        if (!File.Exists(_whisperExecutablePath))
        {
            _logger.LogError("Whisper executable not found at configured path: {Path}", _whisperExecutablePath);
            throw new FileNotFoundException(
                $"Whisper.cpp executable not found at '{_whisperExecutablePath}'. " +
                "Please ensure whisper-cpp is installed and the path is correctly configured in appsettings.json (Whisper:ExecutablePath).");
        }

        // 2. Validate Audio File
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        _logger.LogInformation("Transcribing audio file: {FilePath} with language: {Language}", audioFilePath, language);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _whisperExecutablePath,
                Arguments = $"-f \"{audioFilePath}\" -l {language} --output-txt",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                _logger.LogError("Whisper.cpp exited with code {ExitCode}: {Error}", process.ExitCode, error);
                throw new Exception($"Transcription failed with exit code {process.ExitCode}: {error}");
            }

            var transcription = outputBuilder.ToString().Trim();
            _logger.LogInformation("Transcription completed, length: {Length} characters", transcription.Length);

            return transcription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transcription of file: {FilePath}", audioFilePath);
            throw;
        }
    }
}
