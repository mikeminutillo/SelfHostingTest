using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args);
        }

        public static void BuildWebHost(string[] args)
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var directoryPath = Path.GetDirectoryName(exePath);

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseContentRoot(directoryPath)
                .UseStartup<Startup>()
                .UseSerilog()
                .Build();

            if(Debugger.IsAttached || args.Contains("--debug"))
            {
                host.Run();
            }
            else
            {
                host.RunAsService();
            }
        }
    }
}
