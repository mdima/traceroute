using System;
using System.Diagnostics;

namespace TraceRoute.Services
{
    /// <summary>
    /// Static class to abstract away the complexity of making Bash Command
    /// calls from ASP.NET Core 9.0.
    /// </summary>
    public static class BashService
    {
        /// <summary>
        /// Attempt to run the specified Bash command in async.
        /// </summary>
        /// <returns>Standard output returned from command.</returns>
        /// <param name="cmd">Bash command with arguments.</param>
        public async static Task<string> Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var shellExe = "/bin/bash";
            var shellArgs = $"-c \"{escapedArgs}\"";

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExe,
                    Arguments = shellArgs,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            return result;
        }
    }
}
