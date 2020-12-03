using System;
using System.Collections.Concurrent;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Configuration;

namespace ServiceTest01
{
    // Delegate that defines the signature for the callback method.
    //
    public delegate void MoverCallback(string sourcefilename);

    public static class Logger
    {
        static readonly TextWriter tw;
        private static readonly object _syncObject = new object();

        static Logger()
        {
            //tw = TextWriter.Synchronized(File.AppendText(SPath() + "\\Log.txt"));
            tw = TextWriter.Synchronized(File.AppendText(SPath()));
        }

        public static string SPath()
        {
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            return appSettings["logFileName"];
            //return @"D:\jb\Visual Studio 2019\Projects\ServiceTest01\bin\Debug\ServiceTest01.log";
        }
        
        public static void Log(string logMessage)
        {
            try
            {
                Write(logMessage, tw);
            }
            catch (IOException)
            {
                tw.Close();
            }
        }

        private static void Write(string logMessage, TextWriter w)
        {
            // only one thread can own this lock, so other threads 
            // entering this method will wait here until lock is
            // available.
            lock (_syncObject)
            {
                w.WriteLine("{0} {1} : {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage);
                // Update the underlying file.
                w.Flush();
            }
        }
    }

    class Mover
    {
        private readonly string source;
        private readonly string destination;
        private MoverCallback cbk;
        
        public Mover(string source, string destination, MoverCallback mcbk)
        {
            this.source = source;
            this.destination = destination;
            this.cbk = mcbk;
            Logger.Log("Mover created");
        }

        public void move()
        {
            Thread.Sleep(2000);
            int tries = 0;
            int maxtries = 3;
            while (tries <= maxtries)
            {
                try
                {
                    File.Move(source, destination);
                    Logger.Log($"File moved from  {source} to {destination}");
                    tries = maxtries + 1;
                }
                catch (Exception ex)
                {
                    Logger.Log("Mover " + ex.Message);
                    tries++;
                    Thread.Sleep(1000);
                }
            }
            Thread.Sleep(2000);
            cbk(source);
        }


    }
    public partial class ServiceTest01 : ServiceBase
    {
        private static EventWaitHandle LogWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset, "SHARED_BY_ALL_PROCESSES");
        //private static EventWaitHandle MoveWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset, "SHARED_BY_ALL_PROCESSES");
        //private List<string> FirstEvent;
        private ConcurrentDictionary<string, int> FirstEvent;


        private FileSystemWatcher watcher = null;

        public void callback(string sourcefilnename)
        {
            _ = FirstEvent.TryRemove(sourcefilnename, out int dummy);
            Logger.Log("callback" + sourcefilnename);
        }

        // Define the event handlers.
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            FileAttributes attr = File.GetAttributes(e.FullPath);
            // nur when kein Ordner
            if (!((attr & FileAttributes.Directory) == FileAttributes.Directory))
            {
                Logger.Log(e.ChangeType + " " + e.FullPath + e.ChangeType);
                if (FirstEvent.ContainsKey(e.FullPath))
                {
                    Logger.Log("in der Liste " + e.FullPath);
                    int val = -1;
                    FirstEvent.TryGetValue(e.FullPath, out val);
                    Logger.Log("in der Liste " + e.FullPath + "val = " + val.ToString());
                    if (val == 0)
                    {
                        FirstEvent[e.FullPath] = 1;
                        Mover mv = new Mover(e.FullPath, @"D:\temp\1\_" + e.Name, new MoverCallback(callback));
                        Thread tws = new Thread(new ThreadStart(mv.move));
                        tws.Start();
                        //_ = FirstEvent.TryRemove(e.FullPath, out int dummy);
                    }
                }
                else
                {
                    Logger.Log(">>> in die Liste " + e.FullPath);
                    FirstEvent[e.FullPath] = 0;
                }
            }
        }

        // Create a new FileSystemWatcher and set its properties.
        private void createFileSystemWatcher()
        {
            Logger.Log("createFileSystemWatcher");
            watcher = new FileSystemWatcher(@"D:\temp")
            {
                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.Attributes
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName,
                IncludeSubdirectories = false
            };

            // Only watch text files.
            //watcher.Filter = "*.txt";

            // Add event handlers.
            watcher.Created += OnCreated;
            watcher.Changed += OnCreated;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            Logger.Log("createFileSystemWatcher done");
        }
   
        public ServiceTest01()
        {
            InitializeComponent();
            FirstEvent = new ConcurrentDictionary<string, int>();
            Logger.Log("InitializeComponent");
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log("OnStart");
            this.createFileSystemWatcher();
        }

        protected override void OnStop()
        {
            watcher.Dispose();
            FirstEvent.Clear();
            Logger.Log("OnStop");
        }
    }
}
