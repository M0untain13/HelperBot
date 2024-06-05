namespace ConsoleProject;

public static class Logger
{
	public static void Info(string message) => Out($"Info | {message}");

    public static void Warning(string message) => Out($"Warning | {message}");

    public static void Error(string message) => Out($"Error | {message}");

    private static void Out(string message) => Console.WriteLine($"{DateTime.Now} | {message}");
}