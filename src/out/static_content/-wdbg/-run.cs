//css_args -l:0
using CSScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

static class Scipt
{
    static void Main(string[] args)
    {
        string arg(string name) => args.SkipWhile(x => x != name).Skip(1).Take(1).FirstOrDefault();
        string GetVersion()
            => Path.GetFileNameWithoutExtension(
               Directory.GetFiles(Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")), "*.version")
                        .FirstOrDefault() ?? "0.0.0.0.version");

        if (!args.Any() || args.Contains("?") || args.Contains("-?") || args.Contains("-help"))
        {
            Console.WriteLine($@"v{GetVersion()} ({Environment.GetEnvironmentVariable("EntryScript")})");
            Console.WriteLine("Start web-based debugger (wdbg) for the specified script.");
            Console.WriteLine("  css -wdbg <script_file> [server args]");
            Console.WriteLine();
            Console.WriteLine("You can use CLI arguments to be tunneled to the debugger WebAPI server as in `dotnet run <arguments>`.");
            Console.WriteLine("  Example: `css -wdbg script.cs --urls \"http://localhost:5100;https://localhost:5101\"`");
            return;
        }

        string urls = arg("--urls") ?? Environment.GetEnvironmentVariable("CSS_WEB_DEBUGGING_URL") ?? "http://localhost:5100";

        string script = args.FirstOrDefault()?.GetFullPath();           // first arg is the script to debug

        if (!File.Exists(script))
        {
            if (File.Exists(script + ".cs"))
                script += ".cs";
            else
                throw new Exception($"Cannot find script `{args.FirstOrDefault()}`. Try to use full path.");
        }

        var wdbgDir = Path.GetDirectoryName(Environment.GetEnvironmentVariable("EntryScript")); // EntryScript is set by parent script engine process

        var runAsAssembly = true; // 'dotnet run'
        var serverDir = Path.Combine(wdbgDir, "dbg-server", "output");

        bool forceProjectRun = false;

        if (!Directory.Exists(serverDir) || forceProjectRun)
        {
            runAsAssembly = false; // 'dotnet <assembly>'
            serverDir = Path.Combine(wdbgDir, "dbg-server").GetFullPath();
        }
        Console.WriteLine("serverDir: " + serverDir);

        var serverArgs = "\"" + args.Skip(1).JoinBy("\" \"") + "\"";    // the rest of the args are to be interpreted by the server
        var serverAsm = Path.Combine(serverDir, "server.dll");
        var preprocessor = Path.Combine(wdbgDir, "dbg-inject.cs");

        // start wdbg server
        Process proc;

        if (runAsAssembly)
            proc = "dotnet".Start($"\"{serverAsm}\" -script \"{script}\" --urls \"{urls}\" -pre \"{preprocessor}\" {serverArgs}", serverDir);
        else
            proc = "dotnet".Start($"run -script \"{script}\" --urls \"{urls}\" -pre \"{preprocessor}\" {serverArgs}", serverDir);

        // start browser with wdbg url
        if (!args.Contains("-suppress-browser"))
        {
            Console.WriteLine($"---");

            var url = urls.Split(';').FirstOrDefault();
            Console.WriteLine($"Trying to start the browser at {url}...");
            try
            {
                url.Start(UseShellExecute: true);
            }
            catch { }
            Console.WriteLine($"---");
        }

        proc.WaitForExit();
    }

    static string GetFullPath(this string path) => path != null ? Path.GetFullPath(path) : null;

    static Process Start(this string exe, string args = "", string dir = null, bool UseShellExecute = false)
    {
        Process proc = new();
        proc.StartInfo.FileName = exe;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = UseShellExecute;
        proc.StartInfo.WorkingDirectory = dir;
        proc.Start();
        return proc;
    }
}