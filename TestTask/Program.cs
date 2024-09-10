using TestTask;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: dotnet run <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalInSeconds>");
            return;
        }

        var settings = new Settings(args[0], args[1], args[3]);

        using var logger = new Logger(args[2]);

        var synchronizer = new Synchronizer(logger, settings);
        synchronizer.Start();
    }
}