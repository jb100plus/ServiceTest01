using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace ServiceTest01
{
    // Delegate that defines the signature for the Mover callback method
    public delegate void MoverCallback(string sourcefilename);

    public partial class ServiceTest01 : ServiceBase
    {
        private ConcurrentDictionary<string, int> FirstEvent;
        private FileSystemWatcher watcher = null;
        private readonly string dirToWatch = null;
        private readonly string dirToMove = null;
        private readonly string watchFilter = null;

        public void callback(string sourcefilnename)
        {
            // Eintrag aus der Liste der Dateien löschen
            _ = FirstEvent.TryRemove(sourcefilnename, out int dummy);
            Logger.Log(Logger.LogLevel.DEBUG, "callback" + sourcefilnename);
        }

        public ServiceTest01()
        {
            InitializeComponent();
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            dirToWatch = appSettings["DirectoryToWatch"];
            dirToMove = appSettings["DirectoryToMove"];
            watchFilter = appSettings["WatchFilter"];
            FirstEvent = new ConcurrentDictionary<string, int>();
            Logger.Log(Logger.LogLevel.DEBUG, System.String.Format("{0} {1} {2}", dirToWatch, dirToMove, watchFilter));
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log(Logger.LogLevel.DEBUG, "OnStart");
            this.createFileSystemWatcher();
        }

        protected override void OnStop()
        {
            watcher.Dispose();
            FirstEvent.Clear();
            Logger.Log(Logger.LogLevel.DEBUG, "OnStop");
        }

        // Define the event handlers.
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            // die Events werden für manche Datein unter bstimmten Umständen mehrfach ausgelöst
            // es scheint so zu sein, dass beim Anlegen ein OnCreate-Event ausgelöst wird und dann 
            // mehrere OnChanged Events, abhämgig ist das auch von der Application, die die Datei anlegt
            Logger.Log(Logger.LogLevel.DEBUG, e.ChangeType + " " + e.FullPath);
            FileAttributes attr = File.GetAttributes(e.FullPath);
            // Directories ignorieren
            if (!((attr & FileAttributes.Directory) == FileAttributes.Directory))
            {
                // gab es für diese Datei bereits ein Event
                if (FirstEvent.ContainsKey(e.FullPath))
                {
                    // ja
                    Logger.Log(Logger.LogLevel.DEBUG, "in der Liste " + e.FullPath);
                    int val = -1;
                    FirstEvent.TryGetValue(e.FullPath, out val);
                    Logger.Log(Logger.LogLevel.DEBUG, "in der Liste " + e.FullPath + "val = " + val.ToString());
                    // ist das das zweite Event
                    if (val == 0)
                    {
                        // nur genau beim zweiten Event
                        FirstEvent[e.FullPath] = 1;
                        Mover mv = new Mover(e.FullPath, dirToMove + e.Name, new MoverCallback(callback));
                        Thread tws = new Thread(new ThreadStart(mv.move));
                        tws.Start();
                    }
                    // alle weiteren Events für die Datei werden ignoriert
                    else
                    {
                        Logger.Log(Logger.LogLevel.DEBUG, "ignore " + e.ChangeType + " " + e.FullPath);
                    }
                }
                else
                {
                    // beim ersten Event
                    Logger.Log(Logger.LogLevel.DEBUG, ">>> in die Liste " + e.FullPath);
                    FirstEvent[e.FullPath] = 0;
                }
            }
        }

        // Create a new FileSystemWatcher and set its properties.
        private void createFileSystemWatcher()
        {
            Logger.Log(Logger.LogLevel.DEBUG, "createFileSystemWatcher " + dirToWatch);
            watcher = new FileSystemWatcher(dirToWatch)
            {
                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.Attributes
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName,
                IncludeSubdirectories = false
            };
            watcher.Filter = watchFilter;
            // Add event handlers.
            watcher.Created += OnCreated;
            watcher.Changed += OnCreated;
            // Begin watching.
            watcher.EnableRaisingEvents = true;
            Logger.Log(Logger.LogLevel.DEBUG, "createFileSystemWatcher done");
        }
    }
}
