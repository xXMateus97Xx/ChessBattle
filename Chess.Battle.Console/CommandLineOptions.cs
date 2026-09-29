using System.Buffers;
using System.CommandLine;

namespace Chess.Battle.Console;

internal class CommandLineOptions
{
    private static readonly SearchValues<char> VerboseModes = SearchValues.Create("cCfF");

    public string Engine1Path { get; set; }
    public string Engine1Name { get; set; }
    public string Engine1ConfigPath { get; set; }

    public string Engine2Path { get; set; }
    public string Engine2Name { get; set; }
    public string Engine2ConfigPath { get; set; }

    public string OutputPath { get; set; }
    public int WhiteEngine { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int MoveTimeMs { get; set; }
    public char? Verbose { get; set; }

    public static RootCommand CreateCommand(Action<CommandLineOptions> handler)
    {
        var e1Path = new Option<string>("--e1path") { Required = true, Description = "Set engine 1 path" };
        var e1Name = new Option<string>("--e1name") { Required = true, Description = "Set engine 1 name" };
        var e1Config = new Option<string>("--e1config") { Description = "Path to a key=value file with UCI options for engine 1 (setoption name key value value; # lines are ignored)" };

        var e2Path = new Option<string>("--e2path") { Required = true, Description = "Set engine 2 path" };
        var e2Name = new Option<string>("--e2name") { Required = true, Description = "Set engine 2 name" };
        var e2Config = new Option<string>("--e2config") { Description = "Path to a key=value file with UCI options for engine 2 (setoption name key value value; # lines are ignored)" };

        var output = new Option<string>("--output", "-o") { Required = true, Description = "PGN output path" };
        var white = new Option<int>("--white", "-w") { Description = "Defines wich engine will control the white moves" };
        var timeout = new Option<int>("--timeout", "-t") { Description = "Seconds each engine has to answer a move before being considered unresponsive, 0 for no limit (default 600)" };
        var movetime = new Option<int>("--movetime", "-m") { Required = true, Description = "Milliseconds each engine spends thinking per move (UCI go movetime), used for both engines" };

        var verbose = new Option<string>("--verbose", "-v")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Log the battle: c for console (default when the option is used), f for a Battle-{date}.txt file"
        };
        verbose.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (!string.IsNullOrEmpty(value) && !(value.Length == 1 && VerboseModes.Contains(value[0])))
                result.AddError("Option '--verbose' accepts only 'c' (console) or 'f' (file).");
        });

        var command = new RootCommand("Makes two UCI chess engines play against each other")
        {
            e1Path, e1Name, e1Config,
            e2Path, e2Name, e2Config,
            output, white, timeout, movetime, verbose
        };

        command.SetAction(result =>
        {
            handler(new CommandLineOptions
            {
                Engine1Path = result.GetValue(e1Path),
                Engine1Name = result.GetValue(e1Name),
                Engine1ConfigPath = result.GetValue(e1Config),
                Engine2Path = result.GetValue(e2Path),
                Engine2Name = result.GetValue(e2Name),
                Engine2ConfigPath = result.GetValue(e2Config),
                OutputPath = result.GetValue(output),
                WhiteEngine = result.GetValue(white),
                TimeoutSeconds = result.GetResult(timeout) is null ? null : result.GetValue(timeout),
                MoveTimeMs = result.GetValue(movetime),
                Verbose = result.GetResult(verbose) is null ? null : char.ToLowerInvariant(result.GetValue(verbose)?.FirstOrDefault() ?? 'c')
            });
        });

        return command;
    }
}
