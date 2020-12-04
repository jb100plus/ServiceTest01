using System;
using System.IO;
using System.Text;


namespace ServiceTest01
{
    class RMCSVFile
    {
        private readonly string fileName = null;
        private string line = null;

        public RMCSVFile(string FileName)
        {
            this.fileName = FileName;
        }

        private string GetLine()
        {
            if (line == null)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(fileName))
                    {
                        while (sr.Peek() >= 0)
                        {
                            line = sr.ReadLine();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(Logger.LogLevel.WARNING, String.Format("Kann Zeile nicht lesen {0} {1} ", fileName, e.Message));
                }
            }
            return line;
        }

        private string GetOldTIFFullFileNameCSV()
        {
            string tiffFileName = null;
            try
            {
                var entries = GetLine().Split(';');
                tiffFileName = entries[0];
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("keinen TIF Dateineamen gefunden  {0} {1}", fileName, e.Message));
            }
            return tiffFileName;
        }

        private string GetNewTIFFullFileNameCSV()
        {
            return Path.GetDirectoryName(GetOldTIFFullFileNameCSV()) + "\\" + GetMandant() + "\\" + Path.GetFileName(GetOldTIFFullFileNameCSV());
        }
        
        private string GetNewCSVDirectory()
        {
            return Path.GetDirectoryName(fileName) + "\\" + GetMandant() + "\\";
        }

        private string GetNewLineCSV()
        {
            string newLine = null;
            try
            {
                var entries = GetLine().Split(';');
                string[] newEntries = (string[])entries.Clone();
                newEntries[0] = GetNewTIFFullFileNameCSV();
                StringBuilder sb = new StringBuilder("", 200);
                foreach (string entry in newEntries)
                {
                    sb.Append(entry);
                    sb.Append(";");
                }
                newLine = sb.ToString();
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("kann keine neue Zeile erzeugen {0} {1} ", fileName, e.Message));
            }
            return newLine;
        }

        public string GetMandant()
        {
            string mandantNr = null;
            try
            {
                var entries = GetLine().Split(';');
                mandantNr = entries[6];
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("Keinen Mandant gefunden  {0} {1}", fileName, e.Message));
            }
            return mandantNr;
        }

        public string GetTIFFileName()
        {
            return Path.GetFileName(GetOldTIFFullFileNameCSV());
        }

        public bool SaveAsNewFile()
        {
            bool succes = false;
            try
            {
                string newFileName = GetNewCSVDirectory() + Path.GetFileName(fileName);
                if (!File.Exists(newFileName))
                {
                    using (StreamWriter outputFile = new StreamWriter(newFileName))
                    {
                        outputFile.WriteLine(GetNewLineCSV());
                    }
                    succes = true;
                }
                else
                { 
                    Logger.Log(Logger.LogLevel.INFO, String.Format("SaveAsNewFile Datei existiert bereits {0}", newFileName));
                }
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("SaveAsNewFile fehlgeschlagen {0}", e.Message));
            }
            return succes;
        }

        public bool MoveTIF()
        {
            bool succes = false;
            try
            {
                string newFileName = GetNewCSVDirectory() + Path.GetFileName(GetTIFFileName());
                if (!File.Exists(newFileName))
                {
                    File.Move(Path.GetDirectoryName(fileName) + "\\" + GetTIFFileName(), newFileName);
                    succes = true;
                }
                else
                {
                    Logger.Log(Logger.LogLevel.INFO, String.Format("MoveTIF Datei existiert bereits {0}", newFileName));
                }
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("MoveTIF fehlgeschlagen {0} {1}", GetTIFFileName(), e.Message));
            }
            return succes;
        }

        public bool DeleteOldCSV()
        {
            bool succes = false;
            try
            {
                File.Delete(fileName);
                succes = true;
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.INFO, String.Format("Fehler beim Löschen {0} {1}", fileName, e.Message));
            }
            return succes;
        }
    }
}
