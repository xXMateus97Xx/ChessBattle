using Chess.Battle.Loggers;
using Chess.Battle.NotationWriters;
using Chess.Uci.Connector;

namespace Chess.Battle.Console;

class Program
{
    static int Main(string[] args)
    {
        return CommandLineOptions.CreateCommand(StartBattle).Parse(args).Invoke();
    }

    static void StartBattle(CommandLineOptions options)
    {
        var settings = new BattleSettings
        {
            OutputPath = options.OutputPath,
            WhiteEngine = Math.Max(options.WhiteEngine, 1),
            Engine1 = new EngineSettings
            {
                EnginePath = options.Engine1Path,
                Name = options.Engine1Name,
                Options = LoadEngineOptions(options.Engine1ConfigPath),
            },
            Engine2 = new EngineSettings
            {
                EnginePath = options.Engine2Path,
                Name = options.Engine2Name,
                Options = LoadEngineOptions(options.Engine2ConfigPath),
            },
            MoveTime = TimeSpan.FromMilliseconds(options.MoveTimeMs)
        };

        if (options.TimeoutSeconds is not null)
            settings.Timeout = options.TimeoutSeconds > 0 ? TimeSpan.FromSeconds(options.TimeoutSeconds.Value) : Timeout.InfiniteTimeSpan;

        using ILogger logger = options.Verbose switch
        {
            null => new NullLogger(),
            'f' => new FileLogger(),
            _ => new ConsoleLogger()
        };

        var writer = new PGNWriter();

        var ring = new BattleRing(settings, logger, writer);

        try
        {
            ring.Battle();
        }
        catch (UCIConnectionException ex)
        {
            System.Console.Error.WriteLine(ex.Message);
            if (ex.InnerException != null)
                System.Console.Error.WriteLine(ex.InnerException.Message);
        }

#if DEBUG
        System.Console.ReadLine();
#endif
    }

    private static Dictionary<string, string> LoadEngineOptions(string configPath) =>
        string.IsNullOrEmpty(configPath) ? [] : Settings.LoadOptions(configPath);
}
