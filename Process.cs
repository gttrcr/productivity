using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Process
{
    public static class Process
    {
        public struct OSCommand
        {
            public OSPlatform OSPlatform { get; set; }
            public string? Executable { get; set; }
            public string? Command { get; set; }
        }

        public static List<string> Run(string? executable = null, string? command = null)
        {
            System.Diagnostics.Process process = new();
            if (executable == null)
            {
                OSPlatform os = GetOS();
                if (os.Equals(OSPlatform.Linux))
                {
                    executable = "/bin/bash";
                    command = "-c \"" + command + "\"";
                }
                else
                    throw new PlatformNotSupportedException();
            }

            process.StartInfo.FileName = executable;
            if (!string.IsNullOrEmpty(command))
                process.StartInfo.Arguments = command;

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            StringBuilder stdOutput = new();
            process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data); // Use AppendLine rather than Append since args.Data is one line of output, not including the newline character.

            string? stdError = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(executable, command) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
                return stdOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                    message.AppendLine(stdError);

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }

                throw new Exception(Format(executable, command) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }

        public static bool Exists(string command)
        {
            OSPlatform os = GetOS();
            List<string>? output = null;
            if (os.Equals(OSPlatform.Linux))
                output = Run(null, "command -v " + command);
            else if (os.Equals(OSPlatform.Windows))
                output = Run(null, "WHERE " + command);

            if (output != null && output.Count == 1)
                return File.Exists(output[0]);

            throw new PlatformNotSupportedException();
        }

        public static List<string> Run(List<OSCommand> oSCommands)
        {
            OSPlatform os = GetOS();
            OSCommand oSCommand = oSCommands.First(x => x.OSPlatform.Equals(os));
            return Run(oSCommand.Executable, oSCommand.Command);
        }

        private static string Format(string filename, string? arguments)
        {
            return "[" + filename + (string.IsNullOrEmpty(arguments) ? string.Empty : " " + arguments) + "]";
        }

        private static OSPlatform GetOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                return OSPlatform.FreeBSD;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OSPlatform.Linux;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSPlatform.OSX;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OSPlatform.Windows;

            throw new PlatformNotSupportedException();
        }
    }
}