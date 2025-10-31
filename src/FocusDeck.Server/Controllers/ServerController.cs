using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        [HttpPost("update")]
        public IActionResult UpdateServer()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return BadRequest(new { message = "Server update is only supported on Linux." });
            }

            var scriptFileName = "update-server.sh";
            var scriptPath = Path.Combine(Path.GetTempPath(), scriptFileName);

            var scriptContent = @"
#!/bin/bash
echo 'INFO: Starting FocusDeck Server update...'
cd ~/FocusDeck || { echo 'ERROR: Failed to change directory to ~/FocusDeck. Aborting.'; exit 1; }

echo 'INFO: Pulling latest changes from git...'
git pull origin master || { echo 'ERROR: Failed to pull from git. Aborting.'; exit 1; }

echo 'INFO: Rebuilding the server application...'
cd src/FocusDeck.Server || { echo 'ERROR: Failed to change directory to src/FocusDeck.Server. Aborting.'; exit 1; }
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained -o ~/focusdeck-server || { echo 'ERROR: Failed to publish dotnet application. Aborting.'; exit 1; }

echo 'INFO: Restarting the FocusDeck service...'
sudo systemctl restart focusdeck || { echo 'ERROR: Failed to restart focusdeck service. Check permissions.'; exit 1; }

echo 'SUCCESS: Update script finished. Server has been restarted.'
";
            try
            {
                System.IO.File.WriteAllText(scriptPath, scriptContent.Replace("\r\n", "\n"));

                var chmodProcess = Process.Start("chmod", $"+x {scriptPath}");
                chmodProcess.WaitForExit();

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = scriptPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                
                // Don't wait for exit, as the server will be restarted as part of the script.
                // The script will run in the background.
                return Ok(new { message = "Server update process initiated. The server will restart automatically. Please wait about a minute and then refresh your browser." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
