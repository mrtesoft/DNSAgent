using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Collections.Generic;
namespace DNSAgent
{
	
 
    public class Application
    {
		 static string _AppName;
		public  static string AppName{
			get{
				if(_AppName==null)_AppName=typeof(Application).Assembly.GetName(true).Name;
				return _AppName;
				}
		}
		
		static string _BasePath;
		public  static string BasePath{
			get{
				if(_BasePath==null)_BasePath=System.AppDomain.CurrentDomain.BaseDirectory;
				return _BasePath;}
			set{_BasePath=value;}
			
		}
 
		static Microsoft.Extensions.Configuration.IConfigurationRoot _Config;
		public static Microsoft.Extensions.Configuration.IConfigurationRoot Config { get{return _Config;} }


        private static readonly List<DnsAgent> DnsAgents = new List<DnsAgent>();
        private static readonly DnsMessageCache AgentCommonCache = new DnsMessageCache();
		public static void Main(string[] args)
        {

            
         
            
             
			 
			 Microsoft.Extensions.Configuration.ConfigurationBuilder bconfig = (Microsoft.Extensions.Configuration.ConfigurationBuilder)(new Microsoft.Extensions.Configuration.ConfigurationBuilder());
             //if (args != null)bconfig.AddCommandLine(args).Build();
            bconfig.SetBasePath(BasePath);
            bconfig.AddJsonFile(AppName+".json", optional: true, reloadOnChange: true);
            _Config=bconfig.Build();

            Start(args);
            Config_OnChanged = Config.GetReloadToken().RegisterChangeCallback(Call_Config_OnChanged, new object[]{Config});
         
           
				 
        }
        static Thread _ConsoleTask;
        static bool _ConsoleTask_Exit = false;
        static void ConsoleTask_Run()
        {

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
            var startedEvent = new MrTe.Threading.Tasks.CountdownEvent(listenEndpoints.Length);
            lock (DnsAgents)
            {
                foreach (string listenOn in listenEndpoints)
                {
                    var agent = new DnsAgent(options, rules, listenOn.Trim(), AgentCommonCache);
                    agent.Started += () => startedEvent.Signal();
                    DnsAgents.Add(agent);
                }
            }
           
              
               lock (DnsAgents)
                {
                    foreach (var agent in DnsAgents)
                    {
                        agent.Start();
                    }
                }
               

                startedEvent.Wait();
                Logger.Title = "DNSAgent - Listening ...";
                Logger.Info("DNSAgent has been started.");
                Logger.Info("Press Ctrl-L to clear screen, Ctrl-R to reload configurations, Ctrl-C to stop and quit.");
  
                _ConsoleTask = new  Thread(new ThreadStart(ConsoleTask_Run));
                _ConsoleTask.Start();

          
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
 
        }
        private static void Stop()
        {
            Stop(true);
        }
    
        private static void Reload()
        {
           
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

        private static bool IsEmpty(Microsoft.Extensions.Configuration.IConfigurationSection section)
        {
            foreach (Microsoft.Extensions.Configuration.IConfigurationSection c in section.GetChildren())
            {
                return false;
            }
            return true;
        }

		private static Options ReadOptions()
        {
            Options options = new Options() ;

            if (Config["HideOnStart"] + "" != "") options.HideOnStart = bool.Parse(Config["HideOnStart"] + "");
            if (Config["ListenOn"] + "" != "") options.ListenOn = Config["ListenOn"] + "";
            if (Config["DefaultNameServer"] + "" != "") options.DefaultNameServer = Config["DefaultNameServer"] + "";
            if (Config["QueryTimeout"] + "" != "") options.QueryTimeout = int.Parse(Config["QueryTimeout"] + "");
            if (Config["CompressionMutation"] + "" != "") options.CompressionMutation = bool.Parse(Config["CompressionMutation"] + "");
            if (Config["CacheResponse"] + "" != "") options.CacheResponse = bool.Parse(Config["CacheResponse"] + "");
            if (Config["CacheAge"] + "" != "") options.CacheAge = int.Parse(Config["CacheAge"] + "");



            if (!IsEmpty(Config.GetSection("NetworkWhitelist")))
            {
                options.NetworkWhitelist = new List<string>();
                foreach (Microsoft.Extensions.Configuration.IConfigurationSection section in Config.GetSection("NetworkWhitelist").GetChildren())
                {
                    options.NetworkWhitelist.Add(section.Value + "");
                    
                }

            }
 
 
            return options;
        }

        private static Rules ReadRules()
        {
            Rules rules=new Rules();
          

            if (!IsEmpty(Config.GetSection("Rules")))
            {

                foreach (Microsoft.Extensions.Configuration.IConfigurationSection section in Config.GetSection("Rules").GetChildren())
                {
                   

                    Rule rule = new Rule();
                    
                    if (section["Address"] + "" != "") rule.Address = section["Address"] + "";
                    if (section["CompressionMutation"] + "" != "") rule.CompressionMutation = bool.Parse(section["CompressionMutation"] + "");
                    if (section["ForceAAAA"] + "" != "") rule.ForceAAAA = bool.Parse(section["ForceAAAA"] + "");
                    if (section["NameServer"] + "" != "") rule.NameServer = section["NameServer"] + "";
                    if (section["Pattern"] + "" != "") rule.Pattern = section["Pattern"] + "";
                    if (section["QueryTimeout"] + "" != "") rule.QueryTimeout = int.Parse(section["QueryTimeout"] + "");
                    if (section["UseHttpQuery"] + "" != "") rule.UseHttpQuery = bool.Parse(section["UseHttpQuery"] + "");

                  
                    rules.Add(rule);  
                    



                }
            }


            return rules;
        }

       static IDisposable Config_OnChanged;
        private static void Call_Config_OnChanged(object obj)
    {
        if(Config_OnChanged!=null)Config_OnChanged.Dispose();
         
         
        
		
		Microsoft.Extensions.Configuration.IConfiguration config = (Microsoft.Extensions.Configuration.IConfiguration)((object[])obj)[0];

        Reload();
		 
       /*var appsettings = config.Get<AppSettings>();          
        //TODO:取得最新配置后修改相关业务逻辑
		*/
       
		 
        //重新注册callback，下次appsettings.josn更新后会自动调用
        Config_OnChanged = config.GetReloadToken().RegisterChangeCallback(Call_Config_OnChanged, obj);
    }
		
		
	 
	   
       
		 
    }
}