using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web.Http;
using System.Net.Http.Formatting;

using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;


namespace Termors.Serivces.HippoArduinoSerialDaemon
{
    class Daemon
    {
        private ManualResetEvent _endEvent = new ManualResetEvent(false);

        public static async Task Main(string[] args)
        {
            await new Daemon().Run(args);
        }

        public async Task Run(string[] args)
        { 
            Logger.Log("HippoArduinoSerialDaemon started");

            // Read JSON configuration
            var config = ReadConfig();
           
            // Set up REST services in OWIN web server
            var webapp = WebApp.Start("http://*:9003/", new Action<IAppBuilder>(WebConfig));

            // Open Serial port
            // If this throws, process will end and systemd can restart it.
            // This would typically be for port busy, device not connected etc.
            SerialDaemon.Initialize(config);

            Console.CancelKeyPress += (sender, e) =>
            {
                Logger.Log("HippoArduinoSerialDaemon stopped");
                webapp.Dispose();
            };

            Logger.Log("HippoArduinoSerialDaemon running");


            // Run until Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                _endEvent.Set();
            };

            // Start watchdog
            Watchdog.Dog.ScheduleDog();

            // Start WD keepalive
            await ScheduleWatchdogCheck();

            // Wait for normal termination
            _endEvent.WaitOne();

            Logger.Log("HippoArduinoSerialDaemon ending");
            Environment.Exit(0);        // Normal exit
        }


        // This code configures Web API using Owin
        public void WebConfig(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            // Format to JSON by default
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.EnsureInitialized();

            appBuilder.UseWebApi(config);
        }

        public static Configuration ReadConfig()
        {
            using (StreamReader rea = new StreamReader("configuration.json"))
            {
                string json = rea.ReadToEnd();
                return JsonConvert.DeserializeObject<Configuration>(json);
            }
        }

        private async Task ScheduleWatchdogCheck()
        {
            await Task.Run(async () => await WatchdogCheck());
        }

        private async Task WatchdogCheck()
        {
            // Wait one minute before checking
            bool quit = _endEvent.WaitOne(60000);
            if (quit) return;

            try
            {
                // Attempt to read from serial port
                string retVal = await SerialDaemon.Instance.SendCommand("?T");
                Logger.Log("Watchdog check on serial port: ?T command response {0}", retVal);

                // Trigger Watchdog so it doesn't kick us out.
                // If this loop hangs, the watchdog will exit the process
                // after five minutes, so that systemd (or similar)
                // can restart it
                Watchdog.Dog.Wake();

                await ScheduleWatchdogCheck();

            }
            catch (Exception ex)
            {
                // On error, watchdog is not triggered and process will exit
                Logger.LogError("Error during watchdog check: {0}, {1}", ex.GetType().Name, ex.Message);

                _endEvent.Set();            // Quit even if watchdog doesn't do it for us
            }
        }

    }
}
