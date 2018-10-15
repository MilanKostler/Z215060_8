using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Sablona;
using HMI;
using System.Windows.Threading;

namespace Z215060_8
{
    /// <summary>
    /// Základní třída celé vizualizace pro zakázku Z21????. Vizualizace je Singleton
    /// </summary>
    public class VizualizaceZ215060_8: Vizualizace
    {
        /// <summary>
        /// Objekt reprezentující stroj (Leak tester) a jeho vlastnosti (Stav stroje, Alarmy atd)
        /// </summary>
        public Machine Stroj = new Machine();


        /// <summary>
        /// Role - Vyšší uživatelské oprávnění
        /// </summary>
        public static Role VyssiOpravneni;


        /// <summary>
        /// Hlídá komunikaci s PLC pomocí livebitu - když nebliká, zobrazí se na spodní části hlavní obrazovky nápis, že PLC nekomunikuje
        /// </summary>
        private WatchDog watchDog;
        

        /// <summary>
        /// Hlídá komunikaci s PLC pomocí livebitu - když nebliká, spustí se odpočet pro vypnutí PC
        /// </summary>
        private WatchDog watchDogShutdown;


        /// <summary>
        /// Dialog pro ovládání stroje v režimu seřizování (levá strana)
        /// </summary>
        public WindowSerizovani WinSerizovaniL = null; //!!!po přenesení commands vo vizualizace předělat na private


        /// <summary>
        /// Dialog pro ovládání stroje v režimu seřizování (pravá strana)
        /// </summary>
        public WindowSerizovani WinSerizovaniP = null; //!!!po přenesení commands vo vizualizace předělat na private


        /// <summary>
        /// Reference na hlavní okno vizualizace
        /// </summary>
        private MainWindow mainWinRef;


        private static VizualizaceZ215060_8 instance;
        /// <summary>
        /// Instance singletonu Vizualizace
        /// </summary>
        public static VizualizaceZ215060_8 Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VizualizaceZ215060_8((MainWindow)Application.Current.MainWindow);
                }
                return instance;
            }
        }


        /// <summary>
        /// Privátní konstruktor singletonu Vizualizace. Vytvoří instanci logfilu. 
        /// Bázové třídě Vizualizace vytvoří delegáty pro zobrazení chybové hlášky a zavření hlavného okna.
        /// </summary>
        /// <param name="mainWinRef">Zpětná reference na hlavní okno vizualizace</param>
        private VizualizaceZ215060_8(MainWindow mainWinRef)
            : base(mainWinRef.Dispatcher)
        {
            this.mainWinRef = mainWinRef;

            VyssiOpravneni = new Role("VyssiOpravneni", InitInfo.AdresarDat + SettingsXml, 5);  //Objekt sloužící k přihlášení do vyššího uživatelského oprávnění a odhlášení
        }


        //Inicializace různých komponent. Provádí se jen jednou při startu aplikace, nebo v režimu ladění po stisku příslušných tlačítek
        #region Inicializace


        /// <summary>
        /// Inicializace vizualizace. Při nezdaru vyhodí vyjímku
        /// </summary>
        public void InicializaceVizualizace()
        {
            inicializaceAlarmu();

            mainWinRef.ucVizu.StavStrojeVM = new StavStrojeViewModel(Stroj.Stav);

            //Počítadla
            mainWinRef.ucVizu.ucVizuL.PocitadloVM = new PocitadloViewModel(Stroj.VyrobenoL);
            mainWinRef.ucVizu.ucVizuP.PocitadloVM = new PocitadloViewModel(Stroj.VyrobenoP);
            
            //OPC
            Stroj.InicializaceOpc();
            Stroj.OpcClient.OnChybaKomunikace += new OPCClient.ChybaKomunikaceHandler(opcClient_OnChybaKomunikace);
            Stroj.OpcClient.OnNaplneniDatKOdeslani += () => nastaveniVystupuSerizovani(); //WinSerizovani
            Stroj.OpcClient.OnPrijataData += () => nastaveniVstupuSerizovani(); //WinSerizovani

            if (!InitInfo.Ladeni)
            {
                Stroj.InicializacePTC04();
                Stroj.PripojeniAtequ();
                inicializaceWatchDogu();
                Stroj.OpcClient.Start();
            }
        }


        /// <summary>
        /// Inicializace watchdogů, které budou hlídat, jestli bliká livebit
        /// </summary>
        private void inicializaceWatchDogu()
        {
            watchDog = new WatchDog(8);
            watchDog.OnTimeout += () =>
            {
                mainWinRef.labelErrKomunikacePLC.Visibility = Visibility.Visible;  //Varovná hláška, že PLC nekomunikuje
                Vizualizace.Log(TypUdalosti.Warning, "PLC nekomunikuje");
            };
            watchDog.OnReseted += () => { mainWinRef.labelErrKomunikacePLC.Visibility = Visibility.Collapsed; };
            Stroj.OpcClient.PrijataData.LiveBitCopy.OnChangeValue += (varValue) => { watchDog.Reset(); };           

            watchDogShutdown = new WatchDog(40);
            XmlRW xmlRw = new XmlRW(InitInfo.AdresarDat + SettingsXml);
            if (xmlRw.ReadBool("ShutdounWatchdog", "Enable", true) && !InitInfo.Ladeni)
            {
                watchDogShutdown.OnTimeout += new WatchDog.TimeoutHandler(watchDogShutdown_OnTimeout);  //Vypnutí PC
            }
            Stroj.OpcClient.PrijataData.LiveBitCopy.OnChangeValue += (varValue) => { watchDogShutdown.Reset(); };
        }


        /// <summary>
        /// inicializace alarmů - načtení textů z XML, obsluha události OnNahozenAlarm
        /// </summary>
        private void inicializaceAlarmu()
        {
            mainWinRef.ucAlarmy1.DataContext = Stroj.Alarmy; //Binding Visibility s vlastností JeAlarm 
            mainWinRef.ucAlarmy1.SetListBoxItemSource(Stroj.Alarmy.AktivniAlarmy);
            try
            {
                Stroj.Alarmy.NacistAlarmyZXlm(InitInfo.AdresarAplikace + @"\Data\Alarms.xml");
            }
            catch (Exception ex)
            {
                string strErr = "Nepodařilo se načíst alarmy z XML souboru! " + ex.Message;
                HandlerChyby(strErr, false);
            }
            Stroj.Alarmy.OnNahozenAlarm += (Alarm alarm) =>
                Log(TypUdalosti.Alarm, String.Format("PLC nahodilo alarm č. {0} \"{1}\"", alarm.CisloAlarmu, alarm.Hlaska));  //obsluha události nahození alarmu - zalogování
        }


        #endregion Inicializace



        /// <summary>
        /// Uvolní zdroje. Volat při ukončování aplikace.
        /// </summary>
        public void UvolnitZdroje()
        {
            if (Stroj.PTC04 != null)
            {
                Stroj.PTC04.MeritIdd = false;
                Stroj.PTC04.MeritOut = false;
            }
        }


        /// <summary>
        /// PLC nekomunikuje (Nebliká LiveBit, zobrazí se odpočet pro vypnutí PC)
        /// </summary>
        void watchDogShutdown_OnTimeout()
        {
            Vizualizace.Log(TypUdalosti.Info, "Aktivován odpočet do vypnutí PC");
            WindowShutdown winShutdown = new WindowShutdown(60);
            winShutdown.OnTestKomunikace += () => { return watchDogShutdown.Aktivni; };  //WindowShutdown se pomocí události OnTestKomunikace dotazuje, zda PLC již náhodou opět nekomunikuje
            if (winShutdown.ShowDialog().Value)
            {
                Vizualizace.Log(TypUdalosti.Info, "Vizualizace vypíná PC");
                System.Diagnostics.Process.Start("shutdown", "-s -t 00");
                mainWinRef.Close();
            }
        }



        /// <summary>
        /// Reakce na událost OPC clienta, že došlo k chybě komunikace se serverem. Chybu komunikace PLC s OPC serverem je nutné ošetřit jinak - např. livebitem
        /// </summary>
        private void opcClient_OnChybaKomunikace(string Message, bool autorestart)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (autorestart)
                    Vizualizace.Log(TypUdalosti.Error, Message + ". Pokusím se o restart komunikace.");
                else
                {
                    HandlerChyby("Chyba komunikace s OPC serverem!\n" + Message, true);
                }
            }));
        }


        /// <summary>
        /// Nastavení aktuálních hodnot vstupů windowIO dle přijatých dat z PLC
        /// </summary>
        private void nastaveniVstupuSerizovani()
        {
            OPCZ215060_8 opcClient = Stroj.OpcClient;
            if (!opcClient.PrijataData.EnableManual.Value && !InitInfo.Ladeni) //Zavření formuláře pro seřizování, není-li enable 
            {
                if (WinSerizovaniL != null)
                {
                    WinSerizovaniL.Close();
                    WinSerizovaniL = null;
                }
                if (WinSerizovaniP != null)
                {
                    WinSerizovaniP.Close();
                    WinSerizovaniP = null;
                }
            }

            if (WinSerizovaniL != null && opcClient != null)
            {
                WinSerizovaniL.SetIn(1, opcClient.PrijataData.L.DwManual1.Value);
                WinSerizovaniL.SetIn(2, opcClient.PrijataData.L.DwManual2.Value);
            }

            if (WinSerizovaniP != null && opcClient != null)
            {
                WinSerizovaniP.SetIn(1, opcClient.PrijataData.P.DwManual1.Value);
                WinSerizovaniP.SetIn(2, opcClient.PrijataData.P.DwManual2.Value);
            }
        }


        /// <summary>
        /// Nastavení dat komunikace dle požadovaného stavu z WindowSerizovani
        /// </summary>
        private void nastaveniVystupuSerizovani()
        {
            OPCZ215060_8 opcClient = Stroj.OpcClient;
            opcClient.DataKOdeslani.RucniOvladani.Value = ((WinSerizovaniL != null) || (WinSerizovaniP != null));
            if (WinSerizovaniL != null) //Levá strana
            {
                opcClient.DataKOdeslani.L.DwManual1.Value = WinSerizovaniL.GetOutDWord(1);
                opcClient.DataKOdeslani.L.DwManual2.Value = WinSerizovaniL.GetOutDWord(2);
            }
            else
            {
                opcClient.DataKOdeslani.L.DwManual1.Value = 0;
                opcClient.DataKOdeslani.L.DwManual2.Value = 0;
            }

            if (WinSerizovaniP != null) //Pravá strana
            {
                opcClient.DataKOdeslani.P.DwManual1.Value = WinSerizovaniP.GetOutDWord(1);
                opcClient.DataKOdeslani.P.DwManual2.Value = WinSerizovaniP.GetOutDWord(2);
            }
            else
            {
                opcClient.DataKOdeslani.P.DwManual1.Value = 0;
                opcClient.DataKOdeslani.P.DwManual2.Value = 0;
            }
        }

    }
}
