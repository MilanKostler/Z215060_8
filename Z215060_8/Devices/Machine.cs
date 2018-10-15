using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using LeakTesty;
using Z215060_8.Melexis;
using HMI;
using Sablona;
using System.Windows;

namespace Z215060_8
{
    /// <summary>
    /// Třída reprezentující stroj a jeho vlastnsti
    /// </summary>
    public class Machine
    {
        #region Properties

        /// <summary>
        /// OPC client, obsahuje struktury PrijataData a DataKOdeslani
        /// </summary>
        public OPCZ215060_8 OpcClient;

        /// <summary>
        /// Stav stroje (TotalStop, Ready, Manual...)
        /// </summary>
        public StavStroje Stav = new StavStroje();

        /// <summary>
        /// Správa alarmů stroje, obsahuje kolekci aktivních alarmů
        /// </summary>
        public Alarms Alarmy = new Alarms();

        /// <summary>
        /// Počítadlo vyrobených výrobků vlevo
        /// </summary>
        public Pocitadlo VyrobenoL = new Pocitadlo();

        /// <summary>
        /// Počítadlo vyrobených výrobků vpravo
        /// </summary>
        public Pocitadlo VyrobenoP = new Pocitadlo();

        /// <summary>
        /// Měřicí zařízení Melexis PTC-04
        /// </summary>
        public MelexisPTC04 PTC04;

        /// <summary>
        /// Ateq na levé straně 
        /// </summary>
        public Ateq AteqL;

        /// <summary>
        /// Ateq na pravé straně 
        /// </summary>
        public Ateq AteqP;

        #endregion Properties


        public Machine()
        {

        }


        /// <summary>
        /// Aktualizace alarmů stroje
        /// </summary>
        public void AktualizaceAlarmu()
        {
            Alarmy.SetDWordAlarmu(1, OpcClient.PrijataData.DwErr1.Value);
            Alarmy.SetDWordAlarmu(2, OpcClient.PrijataData.DwErr2.Value);
            Alarmy.SetDWordAlarmu(3, OpcClient.PrijataData.L.DwErr1.Value);
            Alarmy.SetDWordAlarmu(4, OpcClient.PrijataData.L.DwErr2.Value);
            Alarmy.SetDWordAlarmu(5, OpcClient.PrijataData.L.DwErr3.Value);
            Alarmy.SetDWordAlarmu(6, OpcClient.PrijataData.P.DwErr1.Value);
            Alarmy.SetDWordAlarmu(7, OpcClient.PrijataData.P.DwErr2.Value);
            Alarmy.SetDWordAlarmu(8, OpcClient.PrijataData.P.DwErr3.Value);
        }


        /// <summary>
        /// Vytvoření instance Ateq a připojení sériového portu 
        /// </summary>
        public void PripojeniAtequ()
        {
            try
            {
                XmlRW xml = new XmlRW(InitInfo.AdresarDat + Vizualizace.SettingsXml);  //Načtení čísla sériového portu
                string port = xml.ReadString("AteqL", "Port", "COM2");
                AteqL = new Ateq(9600, port);
                AteqL.OnPrijataData += (string data) => Ateq_OnPrijataData(data, Strana.Leva);
                port = xml.ReadString("AteqP", "Port", "COM1");
                AteqP = new Ateq(9600, port);
                AteqP.OnPrijataData += (string data) => Ateq_OnPrijataData(data, Strana.Prava);
            }
            catch (Exception ex)
            {
                VizualizaceZ215060_8.Instance.HandlerChyby("Chyba při inicializaci připojení k přísrtoji ATEQ! " + ex.Message, false);
            }
        }


        /// <summary>
        /// Inicializace měřicího zařízení Melexis PTC-04 (připojení po RS232)
        /// </summary>
        public void InicializacePTC04()
        {
            MainWindow mw = Application.Current.MainWindow as MainWindow;
            PTC04 = new Melexis.MelexisPTC04(mw.Dispatcher) { Kanal = 1 };
            PTC04.OnError += (msg) => VizualizaceZ215060_8.Instance.HandlerChyby(msg, true);
            PTC04.Connect();
        }


        /// <summary>
        /// Reakce na příjem dat z Atequ - poslání dat do PLC a nastartování časovače pro automatické smazání dat
        /// </summary>
        /// <param name="namerenaData"></param>
        private void Ateq_OnPrijataData(string namerenaData, Strana strana)
        {
            if (OpcClient != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 2000;

                if (strana == Strana.Leva) //Levý ateq
                {
                    OpcClient.DataKOdeslani.L.AteqResAvailable.Value = true;
                    OpcClient.DataKOdeslani.L.AteqResult.Value = namerenaData;
                    timer.Elapsed += (se, ea) =>
                    {
                        timer.Stop();
                        if (OpcClient != null)
                        {
                            OpcClient.DataKOdeslani.L.AteqResAvailable.Value = false;
                            OpcClient.DataKOdeslani.L.AteqResult.Value = " ";
                        }
                    };
                    timer.Start();
                }

                if (strana == Strana.Prava) //Pravý ateq
                {
                    OpcClient.DataKOdeslani.P.AteqResAvailable.Value = true;
                    OpcClient.DataKOdeslani.P.AteqResult.Value = namerenaData;
                    timer.Elapsed += (se, ea) =>
                    {
                        timer.Stop();
                        if (OpcClient != null)
                        {
                            OpcClient.DataKOdeslani.P.AteqResAvailable.Value = false;
                            OpcClient.DataKOdeslani.P.AteqResult.Value = " ";
                        }
                    };
                    timer.Start();
                }
            }
        }


        //Obsluha komunikace s PLC na úrovni práce s přijatými a odesílanými daty
        #region KomunikacePLC


        /// <summary>
        /// Připojení k OPC serveru pomocí objektu opcClient a spuštění WatchDogů pro sledování liveBitu !!!upravit komentář
        /// </summary>
        public void InicializaceOpc()
        {
            try
            {
                OpcClient = new OPCZ215060_8(150);
                OpcClient.OnPrijataData += new OPCClient.PrijataDataHandler(opcClient_OnPrijataData);
                OpcClient.OnNaplneniDatKOdeslani += () => obsluhaNaplneniDatKOdeslani();
               //!!! OpcClient.pripojit("opcda://localhost/Kepware.KEPServerEX.V5");
            }
            catch (Exception ex)
            {
                throw new Exception("Nepodařilo se připojit k OPC serveru!\n" + ex.Message);
            }
        }


        /// <summary>
        /// Ošetření události, že byla přijata data
        /// </summary>
        private void opcClient_OnPrijataData()
        {
            MainWindow mw = Application.Current.MainWindow as MainWindow;
            mw.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            {
                obsluhaPrijetiDat();
            }));
        }


        /// <summary>
        /// Odeslání hodnot z přístroje PTC-04 do PLC přes OPC
        /// </summary>
        private void odeslatHodnotyPtc4()
        {
            if ((PTC04 != null) && (PTC04.Connected))
            {
                float? hodnota;
                hodnota = PTC04.IddA.Hodnota;
                OpcClient.DataKOdeslani.L.Ptc4_IDDmA_Valid.Value = (hodnota != null);
                OpcClient.DataKOdeslani.L.IDD_mA.Value = (hodnota != null) ? (float)hodnota.Value : 0;

                hodnota = PTC04.OutA.Hodnota;
                OpcClient.DataKOdeslani.L.Ptc4_Outlsb_Valid.Value = (hodnota != null);
                OpcClient.DataKOdeslani.L.Out_lsb.Value = (Int16)((hodnota != null) ? hodnota.Value : 0);

                hodnota = PTC04.IddB.Hodnota;
                OpcClient.DataKOdeslani.P.Ptc4_IDDmA_Valid.Value = (hodnota != null);
                OpcClient.DataKOdeslani.P.IDD_mA.Value = (hodnota != null) ? (float)hodnota.Value : 0;

                hodnota = PTC04.OutB.Hodnota;
                OpcClient.DataKOdeslani.P.Ptc4_Outlsb_Valid.Value = (hodnota != null);
                OpcClient.DataKOdeslani.P.Out_lsb.Value = (Int16)((hodnota != null) ? hodnota.Value : 0);
            }
        }



        /// <summary>
        /// Naplnění dat objektu Stroj daty přijatými z PLC
        /// </summary>
        private void naplneniDatStroje()
        {
            Stav.SetState(OpcClient.PrijataData.StavStroje.Value);
            VyrobenoL.Ok = OpcClient.PrijataData.L.CountOk.Value;
            VyrobenoL.Nok = OpcClient.PrijataData.L.CountNok.Value;
            VyrobenoP.Ok = OpcClient.PrijataData.L.CountOk.Value;
            VyrobenoP.Nok = OpcClient.PrijataData.L.CountNok.Value;
        }


        /// <summary>
        /// Obsluha přijetí dat z PLC
        /// </summary>
        private void obsluhaPrijetiDat()
        {

            naplneniDatStroje();
            AktualizaceAlarmu();
            //ucVizu.ZobrazitProces(OpcClient.PrijataData); !!! udělat jinak 


            if ((PTC04 != null) && (PTC04.Connected))  //data pro PTC-04
            {
                PTC04.Kanal = OpcClient.PrijataData.Ptc4Kanal.Value ? 2 : 1;
                PTC04.MeritIdd = OpcClient.PrijataData.Ptc4MerIdd.Value;
                PTC04.MeritOut = OpcClient.PrijataData.Ptc4MerOut.Value;
            }

            System.Windows.Input.CommandManager.InvalidateRequerySuggested();    //refresh povolení akcí (commands) - nutno volat např kvůli povolení windowIO
        }


        /// <summary>
        /// Obsluha odeslání sat do PLC
        /// </summary>
        private void obsluhaNaplneniDatKOdeslani()
        {
            OpcClient.DataKOdeslani.LiveBit.Value = !OpcClient.PrijataData.LiveBitCopy.Value; //Negování LiveBitu
            

            odeslatHodnotyPtc4();

            //odeslatRecepturu();
        }


        #endregion KomunikacePLC
    }
}
