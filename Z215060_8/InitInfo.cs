using System;
using System.Text;
using System.IO;

namespace Sablona
{
    /// <summary>
    /// Statická třída, slouží k uchovávání inicializačních hodnot aplikace (cesta adresáře dat apod...) 
    /// </summary>
    static class InitInfo
    {
        public static bool InitDone = false;  //tuto vlastnost lze použít pro detekci, zda již byla provedena inicializace aplikace (probéhá při splashscreenu)
        public static string AdresarDat;
        private static string adresarAplikace;
        public static string AdresarAplikace
        {
            get { return adresarAplikace; }
        }

        private static bool ladeni = false;
        public static bool Ladeni
        {
            get { return ladeni; }
            set 
            {
                if (parDebug == null)   //Parametry spuštění mají přednost
                ladeni = value; 
            }
        }

        private static int parWait;       //Aplikace byla spuštěna s parametrem "/Wait" + čas čekání v milisekundách
        public static int ParWait
        {
            get { return parWait; }
        }

        private static bool? parDebug = null;     //Aplikace byla spuštěna s parametrem "/Debug" (parDebug == true), resp. "/!Debug" (parDebug == false)
        public static bool? ParDebug
        {
            get { return parDebug; }
        }     


        public static void GetInfo()
        {
            parametrySpusteni();
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);   //Pokud zde není SetCurrentDirectory, při spuštění z registrů se CurrentDirectory nastaví na windir
            adresarAplikace = Environment.CurrentDirectory;
            AdresarDat = AdresarAplikace + @"\Data";
        }



        /// <summary>
        /// Načtení parametrů, se kterými byla aplikace spuštěna
        /// </summary>
        private static void parametrySpusteni()
        {
            parWait = 0;
            String[] parametry = Environment.GetCommandLineArgs();
            foreach (string par in parametry)
            {
                string parW = par.Substring(0, 5);
                if (String.Compare(parW, "/Wait", true) == 0)
                {
                    try
                    {
                        string strDelay = par.Substring(5);
                        int delay = int.Parse(strDelay);
                        parWait = delay;
                    }
                    catch
                    {
                    }
                }
                if (String.Compare(par, "/Debug", true) == 0)
                {
                    parDebug = true;
                    ladeni = true;
                }
                if (String.Compare(par, "/!Debug", true) == 0)
                {
                    parDebug = false;
                    ladeni = false;
                }
            }
        }
    }
}
