using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
//using System.Threading.Tasks;
using MrTe.Threading.Tasks;
using System.Windows.Forms;
using DNSAgent;
//using Newtonsoft.Json;
using MrTe.Core.Json;

namespace DNSAgent
{
    internal class Program
    {
        /*
        private const string OptionsFileName = "options.cfg";
        private const string RulesFileName = "rules.cfg";
        */
        static string _AppName;
        public static string AppName
        {
            get
            {
                if (_AppName == null) _AppName = typeof(Program).Assembly.GetName(true).Name;
                return _AppName;
            }
        }

        static string _BasePath;
        public static string BasePath
        {
            get
            {
                if (_BasePath == null) _BasePath = System.AppDomain.CurrentDomain.BaseDirectory;
                return _BasePath;
            }
            set { _BasePath = value; }

        }
        
        private static readonly List<DnsAgent> DnsAgents = new List<DnsAgent>();
        private static readonly DnsMessageCache AgentCommonCache = new DnsMessageCache();
        private static NotifyIcon _notifyIcon;
        private static ContextMenu _contextMenu;
       

        private static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(BasePath);
            
            if (!Environment.UserInteractive) // Running as service
            {
                using (Service service = new Service())
                    ServiceBase.Run(service);
            }
            else // Running as console app
            {
                var parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[]
                        {"/LogFile=", Assembly.GetExecutingAssembly().Location});
                        return;

                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[]
                        {"/LogFile=", "/u", Assembly.GetExecutingAssembly().Location});
                        return;
                }
                Start(args);
            }
        }
        static System.Threading.Thread _ConsoleTask;
        static bool _ConsoleTask_Exit = false;
        static void ConsoleTask_Run(){
           
            while (!_ConsoleTask_Exit)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Modifiers != ConsoleModifiers.Control) continue;
                switch (keyInfo.Key)
                {
                    case ConsoleKey.L:

                        Clear();
                        break;
                    case ConsoleKey.R: // Reload options.cfg and rules.cfg
                        Reload();
                        break;

                    case ConsoleKey.C:
                        _ConsoleTask_Exit = true;
                        Stop();
                        break;
                }
            }
        }
        private static void Start(string[] args)
        {
            Logger.Title = "DNSAgent - Starting ...";

           
            System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildTime = Utils.RetrieveLinkerTimestamp(Assembly.GetExecutingAssembly().Location);
            string programName = "DNSAgent "+version.Major+"."+version.Minor+"."+version.Build+"";
            Logger.Info("{0} (build at {1})\n", programName, buildTime.ToString(CultureInfo.CurrentCulture));
            Logger.Info("Starting...");

            Options options = ReadOptions();
            Rules rules = ReadRules();
            string[] listenEndpoints = options.ListenOn.Split(',');
            var startedEvent = new CountdownEvent(listenEndpoints.Length);
            lock (DnsAgents)
            {
                foreach (string listenOn in listenEndpoints)
                {
                    var agent = new DnsAgent(options, rules, listenOn.Trim(), AgentCommonCache);
                    agent.Started += () => startedEvent.Signal();
                    DnsAgents.Add(agent);
                }
            }
            if (Environment.UserInteractive)
            {
                lock (DnsAgents)
                {
                    if (DnsAgents.Any(agent => !agent.Start()))
                    {
                        PressAnyKeyToContinue();
                        return;
                    }
                }
                startedEvent.Wait();
                Logger.Title = "DNSAgent - Listening ...";
                Logger.Info("DNSAgent has been started.");
                Logger.Info("Press Ctrl-L to clear screen, Ctrl-R to reload configurations, Ctrl-C to stop and quit.");

               /* 
                Task.Run(() =>
                {
                   
                });
                */
                _ConsoleTask = new Thread(new ThreadStart(ConsoleTask_Run));
                _ConsoleTask.Start();

                MenuItem hideMenuItem = new MenuItem(options.HideOnStart ? "Show" : "Hide");
                if (options.HideOnStart)
                    ShowWindow(GetConsoleWindow(), SwHide);
                hideMenuItem.Click += (sender, eventArgs) =>
                {
                    if (hideMenuItem.Text == "Hide")
                    {
                        ShowWindow(GetConsoleWindow(), SwHide);
                        hideMenuItem.Text = "Show";
                    }
                    else
                    {
                        ShowWindow(GetConsoleWindow(), SwShow);
                        hideMenuItem.Text = "Hide";
                    }
                };
                _contextMenu = new ContextMenu(new[]
                {
                     new MenuItem("Clear", (sender, eventArgs) => Clear()),hideMenuItem,
                     new MenuItem("Reload", (sender, eventArgs) => Reload()),
                     new MenuItem("Exit", (sender, eventArgs) => Stop(false))
                });
                _notifyIcon = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    ContextMenu = _contextMenu,
                    Text = programName,
                    Visible = true
                };
                _notifyIcon.MouseClick += (sender, eventArgs) =>
                {
                    if (eventArgs.Button == MouseButtons.Left)
                        hideMenuItem.PerformClick();
                };
                Application.Run();
            }
            else
            {
                lock (DnsAgents)
                {
                    foreach (var agent in DnsAgents)
                    {
                        agent.Start();
                    }
                }
                Logger.Info("DNSAgent has been started.");
            }
        }

        private static void Stop(bool pressAnyKeyToContinue)
        {
            lock (DnsAgents)
            {
                DnsAgents.ForEach(agent =>
                {
                    agent.Stop();
                });
            }
            Logger.Info("DNSAgent has been stopped.");

            if (Environment.UserInteractive)
            {
                Logger.Title = "DNSAgent - Stopped";
                _notifyIcon.Dispose();
                _contextMenu.Dispose();
                if (pressAnyKeyToContinue)
                    PressAnyKeyToContinue();
               
                Application.Exit();
            }
        }
         private static void Stop()
        {
            Stop(true);
         }
         private static void Clear()
         {
             Console.Clear();
         }
        private static void Reload()
        {
            _JConf = null;
            Options options = ReadOptions();
            Rules rules = ReadRules();
            lock (DnsAgents)
            {
                foreach (var agent in DnsAgents)
                {
                    agent.Options = options;
                    agent.Rules = rules;
                }
            }
            AgentCommonCache.Clear();
            Logger.Info("Options and rules reloaded. Cache cleared.");
        }

        private static void PressAnyKeyToContinue()
        {
            Logger.Info("Press any key to continue . . . ");
            Console.ReadKey(true);
        }

        #region Nested class to support running as service

        private class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = "DNSAgent";
            }

            protected override void OnStart(string[] args)
            {
                Start(args);
                base.OnStart(args);
            }

            protected override void OnStop()
            {
                Program.Stop();
                base.OnStop();
            }
        }

        #endregion

        #region Win32 API Import

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwHide = 0;
        private const int SwShow = 5;

        #endregion

        #region Util functions to read rules

        static JsonObject _JConf=null;
        static JsonObject JConf
        {
            get {
                if (_JConf != null) return _JConf;
                string filename = Path.Combine(Environment.CurrentDirectory, AppName + ".json");
           
                if (File.Exists(filename))
                {

                    _JConf = (JsonObject)(new JSONParser()).GetJSONObject(File.ReadAllText(filename));
                }
                return _JConf;
            }
        }
        private static Options ReadOptions()
        {
            Options options = new Options() ;
             
            if (JConf.Contains("HideOnStart")) options.HideOnStart = bool.Parse(JConf["HideOnStart"] + "");
            if (JConf.Contains("ListenOn")) options.ListenOn = JConf["ListenOn"] + "";
            if (JConf.Contains("DefaultNameServer")) options.DefaultNameServer = JConf["DefaultNameServer"] + "";
            if (JConf.Contains("QueryTimeout")) options.QueryTimeout = int.Parse(JConf["QueryTimeout"] + "");
            if (JConf.Contains("CompressionMutation")) options.CompressionMutation = bool.Parse(JConf["CompressionMutation"] + "");
            if (JConf.Contains("CacheResponse")) options.CacheResponse = bool.Parse(JConf["CacheResponse"] + "");
            if (JConf.Contains("CacheAge")) options.CacheAge = int.Parse(JConf["CacheAge"] + "");

            if (JConf.Contains("NetworkWhitelist") && JConf["NetworkWhitelist"] is JsonArray)
            {
                options.NetworkWhitelist = new List<string>();

                foreach (object item in (JsonArray)JConf["NetworkWhitelist"]) {
                    options.NetworkWhitelist.Add(item+"");
                
                }
            
            }


           /* if (File.Exists(Path.Combine(Environment.CurrentDirectory, OptionsFileName)))
            {
                options = JsonConvert.DeserializeObject<Options>(
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, OptionsFileName)));
            }
            else
            {
                options = new Options();
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, OptionsFileName),
                    JsonConvert.SerializeObject(options, Formatting.Indented));
            }*/
            return options;
        }

        private static Rules ReadRules()
        {
            Rules rules=new Rules();
            /*using (
                var file = File.Open(Path.Combine(Environment.CurrentDirectory, RulesFileName), FileMode.OpenOrCreate))
            using (var reader = new StreamReader(file))
            using (var jsonTextReader = new JsonTextReader(reader))
            {
                var serializer = JsonSerializer.CreateDefault();
                rules = serializer.Deserialize<Rules>(jsonTextReader) ?? new Rules();
            }*/
           
            if (JConf.Contains("Rules") && JConf["Rules"] is JsonArray)
            {

                foreach (object item in (JsonArray)JConf["Rules"])
                {
                    JsonObject jobj = (JsonObject)item;

                    Rule rule = new Rule();

                    if (jobj.Contains("Address")) rule.Address = jobj["Address"] + "";
                    if (jobj.Contains("CompressionMutation")) rule.CompressionMutation = bool.Parse(jobj["CompressionMutation"] + "");
                    if (jobj.Contains("ForceAAAA")) rule.ForceAAAA = bool.Parse(jobj["ForceAAAA"] + "");
                    if (jobj.Contains("NameServer")) rule.NameServer = jobj["NameServer"] + "";
                    if (jobj.Contains("Pattern")) rule.Pattern = jobj["Pattern"] + "";
                    if (jobj.Contains("QueryTimeout")) rule.QueryTimeout = int.Parse(jobj["QueryTimeout"] + "");
                    if (jobj.Contains("UseHttpQuery")) rule.UseHttpQuery = bool.Parse(jobj["UseHttpQuery"] + "");


                    rules.Add(rule);  
                    



                }
            }


            return rules;
        }

        #endregion
    }
}