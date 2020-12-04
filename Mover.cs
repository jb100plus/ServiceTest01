using System;
using System.IO;
using System.Threading;

namespace ServiceTest01
{
    class Mover
    {
        private readonly string source;
        private string destination;
        private readonly MoverCallback cbk;

        public Mover(string source, string destination, MoverCallback mcbk)
        {
            this.source = source;
            this.destination = destination;
            this.cbk = mcbk;
            Logger.Log(Logger.LogLevel.DEBUG, String.Format("Mover created {0}", source));
        }

        public void Move()
        {
            // etwas Zeit zum Schreiben/Schliessen der Datei geben
            Thread.Sleep(2000);

            // for Rechnungsmanager csv
            if (Path.GetExtension(source).Equals(".csv"))
            {
                RMCSVFile rp = new RMCSVFile(source);
                string mandant = rp.GetMandant();
                if (null != mandant)
                {
                    Logger.Log(Logger.LogLevel.DEBUG, String.Format("Found mandant {0} {1}", mandant, source));
                    this.destination = Path.GetDirectoryName(source) + "\\" + mandant + "\\" + Path.GetFileName(source);
                }
            }

            int tries = 0;
            int maxtries = 3;
            while (tries < maxtries)
            {
                try
                {
                    File.Move(source, destination);
                    Logger.Log(Logger.LogLevel.INFO, $"File moved from  {source} to {destination}");
                    tries = maxtries + 1;
                }
                catch (Exception ex)
                {
                    tries++;
                    if(tries == maxtries)
                        Logger.Log(Logger.LogLevel.WARNING, String.Format("File {0} not moved, {1}", source, ex.Message));
                    Thread.Sleep(2000);
                }
            }
            cbk(source);
        }
    }

}
