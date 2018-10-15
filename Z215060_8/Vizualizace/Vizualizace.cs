using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using Sablona;

namespace Z215060_8
{
    /// <summary>
    /// V2.0
    /// Základní třída celé vizualizací. Z ní se odvozují třídy pro vizualizace různých zakázek.
    /// </summary>
    public class Vizualizace
    {
        //Konstanty
        #region Constants

        /// <summary>
        /// Název souboru pro nastavení
        /// </summary>
        public const string SettingsXml = "\\Settings.xml";

        #endregion Constants


        //Data
        #region Data

        /// <summary>
        /// Objekt pro logování událostí do textového souboru yyyy_mm.log
        /// </summary>
        private static LogFile logFile;


        /// <summary>
        /// WPF dispatcher pro synchronizaci s hlavním vláknem aplikace
        /// </summary>
        private Dispatcher dispatcher;

        #endregion Data


        //Veřejné metody
        #region Public methods

        /// <summary>
        /// Konstruktor. Vytvoří instanci logfilu
        /// </summary>
        public Vizualizace(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            nacistDataZXml();
            logFile = new LogFile(InitInfo.AdresarDat); //Vytvoření logfilu
        }


        /// <summary>
        /// Zapíše do LogFilu řádek s datem a časem, typem události a textem
        /// </summary>
        /// <param name="typUdalosti"></param>
        /// <param name="textUdalosti">Text události, který se má uložit do logu</param>
        public static void Log(TypUdalosti typUdalosti, string textUdalosti)
        {
            if (logFile != null)
                logFile.ZapsatUdalost(typUdalosti, textUdalosti);
        }


        /// <summary>
        /// Reakce na chybu - vhození hlášky, zapsání do logu, případně ukončení aplikace
        /// </summary>
        /// <param name="hlaska"></param>
        /// <param name="ukonceni"></param>
        public void HandlerChyby(string hlaska, bool ukonceni)
        {
            dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                string strLog = hlaska;
                string strMess = hlaska;
                if (ukonceni)
                {
                    string konec = " Aplikace bude ukončena";
                    strLog += konec;
                    strMess += "\n\n" + konec;
                }
                Vizualizace.Log(TypUdalosti.Error, strLog);
                ShowErrorMsg(strMess);
                if (ukonceni)
                {
                    MainWindow mw = Application.Current.MainWindow as MainWindow;
                    if (mw != null)
                        mw.Close();
                }
            }));
        }


        /// <summary>
        /// Zobrazení chybové hlášky (jinak pokud je aktivní splashscreen a jinak pokud ne)
        /// </summary>
        /// <param name="message"></param>
        public void ShowErrorMsg(string message)
        {
            if (!InitInfo.InitDone)
            {
                Window temp = new Window() { Visibility = Visibility.Hidden }; //jen tímto způsobem docílíme toho, že MessageBox nezmizí pokud je právě zobrazen splashscreen (je to Microsoftí bug)
                temp.Show();
                MessageBox.Show(message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                temp.Close();
            }
            else
            {
                MessageBox.Show(message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #endregion Public methods


        //Soukromé metody
        #region Private methods

        /// <summary>
        /// Načtení inicializačních dat z Init.xml do statické třídy InitInfo
        /// </summary>
        private void nacistDataZXml()
        {
            InitInfo.GetInfo();
            XmlRW initXml = new XmlRW(InitInfo.AdresarDat + "\\Init.xml");   //initInfo.AdresarDat obsahuje dafaultně cestu aplikace/Data

            InitInfo.Ladeni = initXml.ReadBool("bLadeni", "Value", false);
            string defAdrDat = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Deimos";
            InitInfo.AdresarDat = initXml.ReadString("StrAdresarDat", "Value", defAdrDat);
        }

        #endregion Private methods
    }
}
