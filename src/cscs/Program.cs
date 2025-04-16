﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Xml.Linq;
using csscript;
using CSScripting;
using CSScripting.CodeDom;

namespace cscs
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
#if DEBUG
            // Environment.SetEnvironmentVariable("CSS_RESTORE_NUGET_PACKAGES", "true");
            // Environment.SetEnvironmentVariable("CSSCRIPT_NUGET_PACKAGES", @"D:\temp\nuget");
#endif
            try
            {
                try { SetEnvironmentVariable("Console.WindowWidth", Console.WindowWidth.ToString()); } catch { }
                SetEnvironmentVariable("ENTRY_ASM", Assembly.GetEntryAssembly().GetName().Name);
                SetEnvironmentVariable("CSS_ENTRY_ASM", Assembly.GetEntryAssembly().GetName().Name);
                SetEnvironmentVariable("DOTNET_SHARED", typeof(string).Assembly.Location.GetDirName().GetDirName());
                SetEnvironmentVariable("WINDOWS_DESKTOP_APP", Runtime.DesktopAssembliesDir);
                SetEnvironmentVariable("WEB_APP", Runtime.WebAssembliesDir);
                SetEnvironmentVariable("css_nuget", null);

                Runtime.GlobalIncludsDir?.EnsureDir(rethrow: false);
                Runtime.CustomCommandsDir.EnsureDir(rethrow: false);

                var serverCommand = args.LastOrDefault(x => x.StartsWith("-server"));
                var installCommand = args.LastOrDefault(x => x.StartsWith("-install") || x.StartsWith("-uninstall"));

                if (serverCommand.HasText() && !args.Any(x => x == "?"))
                {
                    if (serverCommand == "-server:stop") Globals.StopBuildServer();
                    else if (serverCommand == "-server:start") Globals.StartBuildServer();
                    else if (serverCommand == "-server:restart") Globals.RestartBuildServer();
                    else if (serverCommand == "-server:ping") Globals.Ping();
                    else if (serverCommand == "-server:reset") Globals.ResetBuildServer();
                    else if (serverCommand == "-server:add") Globals.DeployBuildServer();
                    else if (serverCommand == "-server:remove") Globals.RemoveBuildServer();
                    else if (serverCommand == "-servers:start") { Globals.StartRoslynBuildServer(); Globals.StartBuildServer(); }
                    else if (serverCommand == "-servers:stop") { CSScripting.Roslyn.BuildServer.Stop(); Globals.StopBuildServer(); }
                    else if (serverCommand == "-kill") { CSScripting.Roslyn.BuildServer.Stop(); Globals.StopBuildServer(); CSExecutor.Command("list", "kill", "*"); }
                    else if (serverCommand == "-server_r:start") Globals.StartRoslynBuildServer();
                    else if (serverCommand == "-server_r:stop") CSScripting.Roslyn.BuildServer.Stop();
                    else if (serverCommand == "-server_r:start_inproc") CSScripting.Roslyn.BuildServer.Start(); // this command is invisible for users and only used to allow async start of teh server
                    else Globals.PrintBuildServerInfo();
                }
                else if (OSVersion.Platform == PlatformID.Win32NT && installCommand.HasText() && !args.Any(x => x == "?"))
                {
                    if (installCommand == "-install") { Globals.IntegrateWithOS(install: true); }
                    else if (installCommand == "-uninstall") { Globals.IntegrateWithOS(install: false); }
                }
                else
                    CSExecutionClient.Run(args);

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(200);
                    // alive too long on WLS2
                    Process.GetCurrentProcess().Kill(); // some background monitors may keep the app
                });

                return Environment.ExitCode;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }
    }
}