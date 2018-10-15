using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace Sablona
{
    /// <summary>
    /// Typ události, která se má zalogoat
    /// </summary>
    public enum TypUdalosti {Error, Info, Warning, Exception, Alarm};  //TypUdalosti.Exception se používá jen při globálním zachycení neošetřené výjimky

    /// <summary>
    /// Slouží k logování událostí do textového souvoru *.log automaticky generovaného každý měsíc
    /// </summary>
    class LogFile
    {
        string strArdesarDat;
        bool chybaLogovani;


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="strArdesarDat">Cesta k souboru, do kterého se budou logovat události</param>
        public LogFile(string strArdesarDat)
        {
            this.strArdesarDat = strArdesarDat;
            if (!strArdesarDat.EndsWith("\\"))
                this.strArdesarDat = this.strArdesarDat + "\\";
            ulozitDoSouboru("\n");
            chybaLogovani = false;
        }


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="strArdesarDat">Cesta k souboru, do kterého se budou logovat události</param>
        /// <param name="odradkovat">Zda se má v logovacím souboru odřádkovat</param>
        public LogFile(string strArdesarDat, bool odradkovat)
        {
            this.strArdesarDat = strArdesarDat;
            if (!strArdesarDat.EndsWith("\\"))
                this.strArdesarDat = this.strArdesarDat + "\\";
            if (odradkovat)
                ulozitDoSouboru("\n");
            chybaLogovani = false;
        }


        /// <summary>
        /// Uloží do souboru jednu položku
        /// </summary>
        private void ulozitDoSouboru(string strRadek)
        {
            if (chybaLogovani)
                return;
            string strFile;
            strFile = String.Format("{0}{1:yyyy_MM}.log", strArdesarDat, DateTime.Now);

            try
            {
                using (StreamWriter streamWriter = File.AppendText(strFile))
                {
                    streamWriter.WriteLine(strRadek);
                }
            }
            catch (Exception ex)
            {
                chybaLogovani = true;
                MessageBox.Show("Nelze uložit data do logu\n\n" + ex.Message + "\n\nProgram bude ukončen", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                //System.Environment.Exit(0);   //toto je tvrdší způsob ukončení aplikace
            }
        }


        /// <summary>
        /// Zapíše do LogFilu řádek s datem a časem, typem události a textem
        /// </summary>
        /// <param name="hlaska"></param>
        public void ZapsatUdalost(TypUdalosti typUdalosti, string hlaska)
        {
            StringBuilder strRadek = new StringBuilder();
            string oddelovac = "\x09";
            string strTypUdalosti = "";
            switch (typUdalosti)
            {
                case TypUdalosti.Error:
                    strTypUdalosti = "Error";
                    break;
                case TypUdalosti.Info:
                    strTypUdalosti = "Info";
                    break;
                case TypUdalosti.Warning:
                    strTypUdalosti = "Warning";
                    break;
                case TypUdalosti.Exception:
                    strTypUdalosti = "Except.";
                    break;
                case TypUdalosti.Alarm:
                    strTypUdalosti = "Alarm";
                    break;
            }

            strRadek.Append(String.Format("{0:dd.MM.yyyy. HH:mm:ss}", DateTime.Now));
            strRadek.Append(oddelovac);
            strRadek.Append(strTypUdalosti);
            strRadek.Append(oddelovac);
            strRadek.Append(hlaska);
            ulozitDoSouboru(strRadek.ToString());
        }
    }
}
