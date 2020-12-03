using System;
using System.IO;
using System.Threading;

namespace ServiceTest01
{
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
            Logger.Log(Logger.LogLevel.DEBUG, "Mover created");
        }

        public void move()
        {
            // etwas Zeit zum Schreiben/Schliessen der Datei geben
            Thread.Sleep(2000);
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
                        Logger.Log(Logger.LogLevel.WARNING, "Mover " + ex.Message);
                    Thread.Sleep(1000);
                }
            }
            // etwas Zeit, evtl. weitere Events bzgl. dieser Datei abzuwarten
            Thread.Sleep(2000);
            cbk(source);
        }
    }

}
