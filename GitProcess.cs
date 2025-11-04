using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DeployHelper
{
    public static class GitProcess
    {
        public static async Task<int> RunAsync(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(error);
            Console.ResetColor();

            return process.ExitCode;
        }
    }
}
