using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Sablona;
using HMI;
using HMI.Debug;

namespace Z215060_8
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Základní proměnné


        /// <summary>
        /// Základní třída celé vizualizace.
        /// </summary>
        private VizualizaceZ215060_8 vizualizace;

        /*/// <summary>
        /// Role - Vyšší uživatelské oprávnění
        /// </summary>
        public static Role VyssiOpravneni;*/

        /*/// <summary>
        /// Objekt reprezentující stroj (Leak tester) a jeho vlastnosti (Stav stroje, Alarmy atd)
        /// </summary>
        public Machine Stroj = new Machine();*/
        
       /* /// <summary>
        /// Objekt pro logování událostí do textového souboru yyyy_mm.log
        /// </summary>
        private LogFile logFile;       */     
        
       



        #endregion Základní proměnné


        //Commands (akce)
        public static RoutedCommand CommandAbout = new RoutedCommand();
        public static RoutedCommand CommandIO = new RoutedCommand();
        public static RoutedCommand CommandNastaveni = new RoutedCommand();
        public static RoutedCommand CommandVynulovat = new RoutedCommand();
        public static RoutedCommand CommandReceptury = new RoutedCommand();
        public static RoutedCommand CommandVyberRcp = new RoutedCommand();
        public static RoutedCommand CommandLogin = new RoutedCommand();
        public static RoutedCommand CommandLogout = new RoutedCommand();
        public static RoutedCommand CommandAck = new RoutedCommand();
        public static RoutedCommand CommandHome = new RoutedCommand();




        /// <summary>
        /// Konstruktor hlavního okna, při jeho provádění je zobrazen splashscreen
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            vizualizace = VizualizaceZ215060_8.Instance; //Inicializace základní třídy vizualizace
            Vizualizace.Log(TypUdalosti.Info, "Spuštění programu (Verze " + WindowAbout.GetVersion() + ")");

            /*nacistDataZXml();*/

            if (InitInfo.ParWait != 0)  //Prodleva před inicializací (aplikace byla spuštěna s parametrem "/Wait")
                System.Threading.Thread.Sleep(InitInfo.ParWait);

           // VyssiOpravneni = new Role("VyssiOpravneni", InitInfo.AdresarDat + SettingsXml, 5);  //Objekt sloužící k přihlášení do vyššího uživatelského oprávnění a odhlášení
            VizualizaceZ215060_8.VyssiOpravneni.OnAutologout += () => { ((ICommand)CommandLogout).Execute(null); };  //Zavolání commandu logout  Poznámka: pokud by zde bylo this.comLogoutExecute(this, null);, LogOut by se zavolal i když hlavní okno nemá focus. To by ale mohlo způsovit, že by se nějaké modální okno (např WinSeřizování) dospalo při přechodu do celoobrazovkového režimu dolu a aplikace by jakoby vytuhla. 
            akce();

            try
            {
                vizualizace.InicializaceVizualizace();
            }
            catch (Exception ex)
            {
                vizualizace.HandlerChyby("Chyba při inicializaci vizualizace: " + ex.Message, true);
            }

            if (InitInfo.Ladeni)
            {
                Vizualizace.Log(TypUdalosti.Info, "Aktivován režim \"ladění\"");
                this.Title = this.Title + "  ***REŽIM LADĚNÍ***";
            }
            else
                StackPanelLadeni.Visibility = Visibility.Collapsed;

            InitInfo.InitDone = true;
        }



        //Metody pro obsluhu různých událostí (klávesové zkratky, ukonření aplikace, neošetřený vyjímka apod...)
        #region Udalosti

        //Ukončení aplikace
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vizualizace.UvolnitZdroje();
            Vizualizace.Log(TypUdalosti.Info, "Ukončení programu");
        }


        /// <summary>
        /// Otevření hlavního okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!InitInfo.Ladeni)
                if (vizualizace.Stroj.PTC04 == null)
                    vizualizace.Stroj.InicializacePTC04();  //Toto z nějakého důvodu nejde udělat pěhem inicializace aplikace při splashscreenu.
        }


        /// <summary>
        /// Globální ošetření vyjímek, na které programátor zapomněl
        /// </summary>
        public void GlobalExceptionHandler(Exception e)
        {
            Vizualizace.Log(TypUdalosti.Exception, e.Message);

            if (vizualizace != null)
                vizualizace.ShowErrorMsg(e.Message + "\n\nAplikace bude ukončena.");
            this.Close();
        }




        /// <summary>
        /// Stisknuto tlačítko pro změnu hesla
        /// </summary>
        private void menBtnChangePsw_Click(object sender, RoutedEventArgs e)
        {
            WinZmenaHesla wzh = new WinZmenaHesla();
            wzh.OnPasswordOk += (heslo) => { return VizualizaceZ215060_8.VyssiOpravneni.OvereniHesla(heslo); };
            if (wzh.ShowDialog().Value)
                VizualizaceZ215060_8.VyssiOpravneni.SaveHashHelsa(wzh.passwordBoxNoveHeslo1.Password);
        }


        /// <summary>
        /// Klávesová zkratka pro ovládání Deimos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.X)
            {
                ModifierKeys shiftCtrl = ModifierKeys.Shift | ModifierKeys.Control;
                if (Keyboard.Modifiers == shiftCtrl)
                    StackPanelLadeni.Visibility = Visibility.Visible;
            }
        }

        #endregion Udalosti



        //Centralizovaná správa akcí, které se spouštějí z menu (klávesové zkratky, enable/disable apod.)
        #region Commands

        /// <summary>
        /// Vytvoření a přiřazení commands tlačítkům, klávesové zkratky 
        /// </summary>
        private void akce()
        {
            CommandBinding cb = new CommandBinding(CommandAbout, comAboutExecute, comAboutCanExecute);
            this.CommandBindings.Add(cb);

            toolBtnAbout.Command = CommandAbout;
            MenBtnAbout.Command = CommandAbout;

                //klávesové zkratky
            KeyGesture kg = new KeyGesture(Key.F1, ModifierKeys.Control);
            InputBinding ib = new InputBinding(CommandAbout, kg);
            this.InputBindings.Add(ib);

            //I/O
            cb = new CommandBinding(CommandIO, comIOExecute, comIOCanExecute);
            this.CommandBindings.Add(cb);

            menBtnIO.Command = CommandIO;
            toolBtnIO.Command = CommandIO;

            kg = new KeyGesture(Key.F4);
            ib = new InputBinding(CommandIO, kg);
            this.InputBindings.Add(ib);

            //Nastavení
            cb = new CommandBinding(CommandNastaveni, comNastaveniExecute, comNastaveniCanExecute);
            this.CommandBindings.Add(cb);

            menBtnNastaveni.Command = CommandNastaveni;
            toolBtnNastaveni.Command = CommandNastaveni;

            kg = new KeyGesture(Key.F3);
            ib = new InputBinding(CommandNastaveni, kg);
            this.InputBindings.Add(ib);

            //Vynulovat
            cb = new CommandBinding(CommandVynulovat, comVynulovatExecute, comVynulovatCanExecute);
            this.CommandBindings.Add(cb);

            menBtnVynulovat.Command = CommandVynulovat;
            toolBtnVynulovat.Command = CommandVynulovat;

            kg = new KeyGesture(Key.F7);
            ib = new InputBinding(CommandVynulovat, kg);
            this.InputBindings.Add(ib);

            //Receptury
            cb = new CommandBinding(CommandReceptury, comRecepturyExecute, comRecepturyCanExecute);
            this.CommandBindings.Add(cb);

            menBtnReceptury.Command = CommandReceptury;
            toolBtnReceptury.Command = CommandReceptury;

            kg = new KeyGesture(Key.F10);
            ib = new InputBinding(CommandReceptury, kg);
            this.InputBindings.Add(ib);

            /*  //Vyber receptury
              cb = new CommandBinding(CommandVyberRcp, comVyberRcpExecute, comVyberRcpCanExecute);
              this.CommandBindings.Add(cb);

              menBtnVyberRcp.Command = CommandVyberRcp;
              toolBtnVyberRcp.Command = CommandVyberRcp;
              ucVizualizace.btnTypeName.Command = CommandVyberRcp;

              kg = new KeyGesture(Key.F11);
              ib = new InputBinding(CommandVyberRcp, kg);
              this.InputBindings.Add(ib);   */

            //Login
            cb = new CommandBinding(CommandLogin, comLoginExecute, comLoginCanExecute);
            this.CommandBindings.Add(cb);

            menBtnLogin.Command = CommandLogin;
            toolBtnLogin.Command = CommandLogin;

            kg = new KeyGesture(Key.F5, ModifierKeys.Control);
            ib = new InputBinding(CommandLogin, kg);
            this.InputBindings.Add(ib);

            //Logout
            cb = new CommandBinding(CommandLogout, comLogoutExecute, comLogoutCanExecute);
            this.CommandBindings.Add(cb);

            menBtnLogout.Command = CommandLogout;
            toolBtnLogout.Command = CommandLogout;

            kg = new KeyGesture(Key.F5);
            ib = new InputBinding(CommandLogout, kg);
            this.InputBindings.Add(ib);

            //ACK
            cb = new CommandBinding(CommandAck, comAckExecute, comAckCanExecute);
            this.CommandBindings.Add(cb);

            menBtnAck.Command = CommandAck;
            toolBtnAck.Command = CommandAck;

            kg = new KeyGesture(Key.F6, ModifierKeys.Control);
            ib = new InputBinding(CommandAck, kg);
            this.InputBindings.Add(ib);

         /*   //Home
            cb = new CommandBinding(CommandHome, comHomeExecute, comHomeCanExecute);
            this.CommandBindings.Add(cb);

            menBtnHome.Command = CommandHome;
            toolBtnHome.Command = CommandHome;

            kg = new KeyGesture(Key.F7, ModifierKeys.Control);
            ib = new InputBinding(CommandHome, kg);
            this.InputBindings.Add(ib);

          */
        }


        /// <summary>
        /// Zobrazí se návod k aplikaci v nainstalovaném PDF prohlížeči
        /// </summary>
        private void comNnavodExecute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(InitInfo.AdresarAplikace + @"\Data\Navod.pdf");
                Vizualizace.Log(TypUdalosti.Info, "comNnavodExecute");
            }
            catch (Exception ex)
            {
                Vizualizace.Log(TypUdalosti.Error, "Nepodařilo se otevřít soubor s návodem. Message: " + ex.ToString());
                MessageBox.Show("Nepodařilo se otevřít soubor s návodem\n\n" + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }
        private void navodCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }


        /// <summary>
        /// Zobrazení informací o programu (verze a pod...)
        /// </summary>
        private void comAboutExecute(object sender, ExecutedRoutedEventArgs e)
        {
            Vizualizace.Log(TypUdalosti.Info, "comAboutExecute");
            WindowAbout aboutDlg = new WindowAbout();
            aboutDlg.ShowDialog();
        }
        private void comAboutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        

        /// <summary>
        /// Vytvoření a zobrazení okna pro ovládání stroje v režimu seřizování, načtení textů z XML, obsluha událostí...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comIOExecute(object sender, ExecutedRoutedEventArgs e)
        {
            Strana str = WinVyberStrany.VyberStrany(false);
            bool rightSide = ((str & Strana.Prava) != 0);

            string strXml = @"\Data\SerizovaniL.xml";
            WindowSerizovani winSerizovani = vizualizace.WinSerizovaniL;
            if (rightSide)
                strXml = @"\Data\SerizovaniP.xml";

            if ((str != Strana.Zadna) && (InitInfo.Ladeni || (vizualizace.Stroj.OpcClient != null && vizualizace.Stroj.OpcClient.PrijataData.EnableManual.Value)))
            {
                try
                {
                    Vizualizace.Log(TypUdalosti.Info, "Aktivován režim seřizování");
                    winSerizovani = new WindowSerizovani(InitInfo.AdresarAplikace + strXml);
                    winSerizovani.Title = rightSide ? "Seřizování - Pravá strana" : "Seřizování - Levá strana"; 
                    if (rightSide)
                        vizualizace.WinSerizovaniP = winSerizovani;
                    else
                        vizualizace.WinSerizovaniL = winSerizovani;
                    winSerizovani.ShowDialog();
                    vizualizace.WinSerizovaniL = null;
                    vizualizace.WinSerizovaniP = null;
                }
                catch (Exception ex)
                {
                    try { winSerizovani.Close(); }
                    catch { }
                    winSerizovani = null;
                    string strErr = "Nepodařilo se vytvořit formulář vstupů/výstupů. ";
                    vizualizace.HandlerChyby(strErr + "Message: " + ex.Message, false);
                }
            }
        }
        private void comIOCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = InitInfo.Ladeni || ((vizualizace.Stroj.OpcClient != null && (vizualizace.Stroj.OpcClient.PrijataData.EnableManual.Value)));
        }


        /// <summary>
        ///Ack
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comAckExecute(object sender, ExecutedRoutedEventArgs e)
        {
            vizualizace.Stroj.OpcClient.DataKOdeslani.Ack.Value = true;
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(1);
            dt.Tick += (se, ea) =>
            {
                vizualizace.Stroj.OpcClient.DataKOdeslani.Ack.Value = false;
                dt.Stop();
            };
            dt.Start();
        }
        private void comAckCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ((vizualizace.Stroj.OpcClient != null) && (vizualizace.Stroj.Alarmy.JeAlarm));  
        }


        /// <summary>
        /// Přihlášení do vyššího uživatelského oprávnění
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comLoginExecute(object sender, ExecutedRoutedEventArgs e)
        {
            string psw = WinLogin.GetHeslo();

            if (psw != null)
            {
                if (VizualizaceZ215060_8.VyssiOpravneni.LogOn(psw))
                {
                    menBtnLogin.IsEnabled = false;
                    menBtnLogout.IsEnabled = true;
                    toolBtnLogin.Visibility = Visibility.Collapsed;
                    toolBtnLogout.Visibility = Visibility.Visible;
                    //celoobrazovkovyMod(false);
                }
                else
                {
                    MessageBox.Show("Zadáno špatné heslo");
                }
            }
        }
        private void comLoginCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        /// <summary>
        /// Odhlášení z vyššího uživatelského oprávnění
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comLogoutExecute(object sender, ExecutedRoutedEventArgs e)
        {
            VizualizaceZ215060_8.VyssiOpravneni.Logout();
            menBtnLogin.IsEnabled = true;
            menBtnLogout.IsEnabled = false;
            toolBtnLogin.Visibility = Visibility.Visible;
            toolBtnLogout.Visibility = Visibility.Collapsed;
            CommandManager.InvalidateRequerySuggested();    //refresh povolení akcí (commands)
            /*if (!InitInfo.Ladeni)
                celoobrazovkovyMod(true);*/
        }
        private void comLogoutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        /// <summary>
        /// Nastavení zařízení a aplikace
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comNastaveniExecute(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Na této funkci se ještě ani nepracuje!!!");
           /* WindowNastaveni.NacistNastaveni(InitInfo.AdresarDat + SettingsXml);
            WindowNastaveni winNast = new WindowNastaveni();
            winNast.OnOdecteniHmotnosti += () => { return vaha.Hmotnost; };  //Ošetření události, že si okno nastavení řeklo o aktuální hmotnost na váze
            if (opcClient != null)
                opcClient.DataKOdeslani.OtevrenDialog.Value = true;
            if (winNast.ShowDialog().Value)
            {
                logFile.ZapsatUdalost(TypUdalosti.Info, "Uloženo nastavení aplikace");
                bool lzeOdebrat = vaha.LzeOdebrat;  //Váha
                vaha = new Vaha(WindowNastaveni.HmotnostBedny, actRecepture.Receptura.HmotnostVyrobkuG, WindowNastaveni.MaxPocetNok, lzeOdebrat);

                if (ultrazvuk != null)
                    ultrazvuk.Vyhodnocovat = WindowNastaveni.UltrazvukEnableResult;

                if (!InitInfo.Ladeni)  //Vreos - odpojení či připojení dle potřeby
                {
                    if (WindowNastaveni.UsingVreos)
                    {
                        if (komunikaceVreos == null)
                            inicializaceVreosu();
                    }
                    else
                    {
                        if (komunikaceVreos != null)
                        {
                            komunikaceVreos.Odpojit();
                            komunikaceVreos = null;
                        }
                    }
                }
            }
            if (opcClient != null)
                opcClient.DataKOdeslani.OtevrenDialog.Value = false;*/
        }
        private void comNastaveniCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (VizualizaceZ215060_8.VyssiOpravneni.LoggedOn && (vizualizace.Stroj.OpcClient != null && vizualizace.Stroj.OpcClient.PrijataData.EnableNastaveni.Value)) || InitInfo.Ladeni;
        }


        /// <summary>
        /// Vynulování statistik
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comVynulovatExecute(object sender, ExecutedRoutedEventArgs e)
        {
            Strana str = WinVyberStrany.VyberStrany(true);
            if (str != Strana.Zadna)
            {
                MessageBoxResult mBRes = MessageBox.Show("Chcete opravdu vynulovat statistiky?", "Vynulování", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (mBRes == MessageBoxResult.Yes)
                {
                    vizualizace.Stroj.OpcClient.DataKOdeslani.L.ResetStatistik.Value = ((str & Strana.Leva) != 0);
                    vizualizace.Stroj.OpcClient.DataKOdeslani.P.ResetStatistik.Value = ((str & Strana.Prava) != 0);
                    DispatcherTimer dt = new DispatcherTimer();
                    dt.Interval = TimeSpan.FromSeconds(2);
                    dt.Tick += (se, ea) =>
                    {
                        vizualizace.Stroj.OpcClient.DataKOdeslani.L.ResetStatistik.Value = false;
                        vizualizace.Stroj.OpcClient.DataKOdeslani.P.ResetStatistik.Value = false;
                        dt.Stop();
                    };
                    dt.Start();
                    Vizualizace.Log(TypUdalosti.Info, "Vynulovány statistiky");
                }
            }
        }
        private void comVynulovatCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        /// <summary>
        /// Zobrazení receptur
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comRecepturyExecute(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Na této funkci se ještě ani nepracuje!!!");
            /*WinReceptury wr = new WinReceptury();
            wr.OnError += (message) => { handlerChyby(message, false); };
            if (opcClient != null)
                opcClient.DataKOdeslani.OtevrenDialog.Value = true;
            wr.ShowDialog();
            if (opcClient != null)
                opcClient.DataKOdeslani.OtevrenDialog.Value = false;
            if (actRecepture != null)
                aktualizaceReceptury(actRecepture.Receptura.RecepturaID);
            string druhaStrana;
            if (WindowNastaveni.StranaLt == Strana.Leva)
                druhaStrana = Strana.Prava.ToString();
            else
                druhaStrana = Strana.Leva.ToString();
            if (msgClient != null)
                msgClient.PoslatZpravu(msgAktualizujDb, druhaStrana);*/
        }
        private void comRecepturyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (VizualizaceZ215060_8.VyssiOpravneni.LoggedOn && (vizualizace.Stroj.OpcClient != null && vizualizace.Stroj.OpcClient.PrijataData.EnableNastaveni.Value)) || InitInfo.Ladeni;
        }

        #endregion Commands



        //Reakce na stisk tlačítek, které jsou v normálním režimu neviditelné, slouží jen k ladění aplikace
        #region Ladění

        private void btnSkryt_Click(object sender, RoutedEventArgs e)
        {
            StackPanelLadeni.Visibility = Visibility.Collapsed;
        }

        private void btnHodnoty_Click(object sender, RoutedEventArgs e)
        {
            vizualizace.Stroj.Alarmy.SetDWordAlarmu(1, 0xF);
            vizualizace.Stroj.Alarmy.SetDWordAlarmu(32, 0x100);
            vizualizace.Stroj.Stav.SetState(StavyStroje.Pripraven);
            vizualizace.Stroj.VyrobenoL.Ok = 55;
            vizualizace.Stroj.VyrobenoL.Nok = 1;
            vizualizace.Stroj.VyrobenoP.Ok = 109;
            vizualizace.Stroj.VyrobenoP.Nok = 2;


           /* byte test1 = vyhodnoceniToByte(true, false);
            byte test2 = vyhodnoceniToByte(false, true);
            byte test3 = vyhodnoceniToByte(false, false);
            ucVizualizace.ZobrazitProces(8, 2, 100, 1, test1, test2, 0, 0, 0, false, false, true, false, false, true);
            uCGlobalInfo1.ZobrazitInfo((vaha.HmotnostKg + " kg"), vaha.PocetVyrobku, vaha.Plna, false, true, true);
            ucAlarmy1.SetDWordAlarmu(1, 6);
            uCPlneniPlyn.Animovat = !uCPlneniPlyn.Animovat;
            uCPlneniVoda.Animovat = !uCPlneniPlyn.Animovat;
            */

            /*Strana str = WinVyberStrany.VyberStrany(true);
            if ((str & Strana.Leva) != 0)
                MessageBox.Show("Levá");*/



            /*DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(5);
            dt.Tick += (se, ea) =>
            {
                if (winSerizovaniL != null)
                {
                    winSerizovaniL.Close();
                    winSerizovaniL = null;
                }
                if (winSerizovaniP != null)
                {
                    winSerizovaniP.Close();
                    winSerizovaniP = null;
                };
            };
            dt.Start();*/
        }


        private void btnOpcConnect_Click(object sender, RoutedEventArgs e)
        {
            vizualizace.Stroj.OpcClient.Start();
        }


        private void btnOpcDebug_Click(object sender, RoutedEventArgs e)
        {
            if (vizualizace.Stroj.OpcClient != null)
            {
                WinPlcCommunicationDebug wpcd = new WinPlcCommunicationDebug();
                wpcd.Width = 1500;
                wpcd.PlcVariablesVM = new PlcVariablesViewModel(vizualizace.Stroj.OpcClient);
                wpcd.Show();
            }
        }


        private void Melexis_Click(object sender, RoutedEventArgs e)
        {
            if (vizualizace.Stroj.PTC04 == null)
            {
                vizualizace.Stroj.InicializacePTC04();
            }

            vizualizace.Stroj.PTC04.MeritIdd = checkBoxIdd.IsChecked.Value;
            vizualizace.Stroj.PTC04.MeritOut = checkBoxOut.IsChecked.Value;
        }

        #endregion Ladění


    }
}
