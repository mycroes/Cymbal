public static class ProcessRunner
{
    public static List<string> Execute(string command, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        var errorBuilder = new StringBuilder();
        var output = new List<string>();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                output.Add(args.Data);
            }
        };
        process.BeginOutputReadLine();
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };
        process.BeginErrorReadLine();

        var commandLineMessage = $@"Command line: {command} {arguments}.";

        if (!process.DoubleWaitForExit())
        {
            var timeoutError = $@"Process timed out. {commandLineMessage}.
Output: {string.Join(Environment.NewLine, output)}
Error: {errorBuilder}";
            throw new Error(timeoutError);
        }

        if (process.ExitCode == 0)
        {
            return output;
        }

        var errors = errorBuilder.ToString();
        if (errors.Contains("Cannot find a tool in the manifest file that has a command named"))
        {
            var message = $"""
            The dotnet tool `dotnet-symbol` was not found.
            {commandLineMessage}
            To install, run in the root of the repository
                dotnet new tool-manifest
                dotnet tool install dotnet-symbol
            """;
            throw new Error(message);
        }

        var error = $@"Could not execute process. {commandLineMessage}.
Output: {string.Join(Environment.NewLine, output)}
Error: {errors}";
        throw new Error(error);
    }
}