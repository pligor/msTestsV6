using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyTests.libs;

public static class TerminalHelper
{
  public static async Task<string> RunCommand(string command)
  {
    var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    using (var process = new Process { StartInfo = processInfo })
    {
      process.Start();
      string output = await process.StandardOutput.ReadToEndAsync();
      string error = await process.StandardError.ReadToEndAsync();
      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        throw new InvalidOperationException($"Command '{command}' failed with error: {error}");
      }

      return output;
    }
  }
}
