namespace GentleHealersApi.Helpers;

using Serilog;
public static class Log
{
    public static ILogger Logger { get; }
    static Log()
    {
        // Configureer de logger om naar zowel de console als een bestand te schrijven
        Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}]{ Message: lj}{ NewLine}{ Exception}")
    .WriteTo.File("TestExecutionLogs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
    }
}



