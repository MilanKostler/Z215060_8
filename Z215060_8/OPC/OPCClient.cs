//******************************************************************************
//      VERZE 4.0 - ItemBit32R/W, který se komunikuje přes ItemBit32R/WContainer 
//      VERZE 3.1 - Přejmenování ItemW.GetWValue() na ItemW.GetWValueToClient()
//                  a Parse() na SetRValueFromClient()
//      VERZE 3.0 - Implementace interface IGetReadWriteCollections
//      VERZE 2.1 - Vygenerování PlcVariablesViewModelu pro ladění komunikace
//      VERZE 2.0 - Items s Write modem
//******************************************************************************
//    Tato třída (OPC klient) slouží pro komunikaci s OPC serverem. Ovládá se
//      pomocí metod "pripojit", "start" a "stop". Klient po vytvoření, připojení
//      k serveru a spuštění neustále cyklicky čte a zapisuje hodnoty na server.
//      Hodnoty (items) jsou rozděleny do dvou skupin (groups) pro čtení a zápis.
//    Pokud dojde k chybě komunikace se serverm, vyhodí událost OnChybaKomunikace
//      a je nutné jej znovu připojit a spustit.
//    "OPCClient" je napsán jako obecný program do všech strojů, nezávislý
//      na datech které se posílají a zbytku programu. Při opakovaném použití se
//      do kódu této třídy NEMUSÍ VŮBEC ZASAHOVAT. Zpracování přijatého resp.
//      vytvoření odesílaného bufferu je práce pro třídu odvozenou, která musí
//      vytvořit alespoň jednu instanci třidy odvozené z Item a přidat jí ve svém
//      konstruktoru do Dictionary itemsCteni a alespoň jednu pro zápis
//      do Dictionary itemsZapis.
//    Tato třída také generuje události (např. že byla přijata data), které
//      musí obsloužit program, který tuto třídu používá.
//
//    Nastavení projektu v MSVS2010:
//      - Platform target: x86
//      - References - OpcNetApi.dll, OpcNetApi.Com.dll
//      - V Solution Exploreru založit New Folder, přetáhnout do něj všechny DLL
//          knihovny a zkontrolovat, že mají ve vlastnostech nastaveno
//          Build Action: Content. (Aby se při rebuild dostaly do ..bin\debug)
//
//******************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Collections.ObjectModel;
using HMI.Debug;

namespace HMI
{
    /// <summary>
    /// Slouží ke komunikaci s OPC serverem. Zajišťuje čtení a zápis dat, aniž by věděla co znamenají.
    /// Implementuje rozhraní pro zobrazení OPC proměnných
    /// </summary>
    public abstract class OPCClient : IGetReadWriteCollections
    {
        private bool writeAll;
        /// <summary>
        /// Pokud je true, zapisují se při zápisu všechny ItemW, které mají vlastnost WriteMode WriteMode == WMode.Changed,
        /// bez ohledu na to, zda došlo ke změně jejich hodnoty či nikoli.
        /// WriteAll lze jen nahodit do true, po úspěšném zápisu se automaticky zresetuje.
        /// </summary>
        public bool WriteAll
        {
            get { return writeAll; }
            set { writeAll = writeAll || value; }
        }

        private bool run;
        /// <summary>
        /// Příznak, že klient cyklicky čte a zapisuje na OPC server (nebo se o to stále pokouší)
        /// </summary>
        public bool Run { get { return run; } }

        /// <summary>
        /// Pomocná hashtabulka items pro čtení
        /// </summary>
        protected Dictionary<string, ItemR> itemsCteni = new Dictionary<string, ItemR>();

        /// <summary>
        /// Pomocná hashtabulka items pro zápis
        /// </summary>
        protected Dictionary<string, ItemW> itemsZapis = new Dictionary<string, ItemW>();

        /// <summary>
        /// Kolekce Opc.Da.Item pro čtení, se kterou pracují klientské DLL
        /// </summary>
        protected Opc.Da.Subscription groupCteni;

        /// <summary>
        /// Kolekce Opc.Da.Item pro zápis, se kterou pracují klientské DLL
        /// </summary>
        protected Opc.Da.Subscription groupZapis;

        /// <summary>
        /// Třída pro spojení s OPC serverem
        /// </summary>
        private Opc.Da.Server server;

        private bool firstScanOn;
        /// <summary>
        /// Shodí se po prvním přečtení dat a vyvolání události OnPrijataData()
        /// </summary>
        public bool FirstScanOn
        {
            get { return firstScanOn; }
        }

        /// <summary>
        /// Již byl proveden 1 opravný pokus načtení a zápisu dat při chybě
        /// </summary>
        private bool restarted;

        private DispatcherTimer timerReadFromOpc;
        private DispatcherTimer timerWriteToOpc;
        private DispatcherTimer timerReadTimeout;
        private DispatcherTimer timerWriteTimeout;
        private DispatcherTimer timerRestart; //Timer pro automatický pokus o restart komunikace

        //deklarace událostí
        public delegate void ChybaKomunikaceHandler(string Message, bool autorestart);
        public event ChybaKomunikaceHandler OnChybaKomunikace;
        public delegate void KomunikaceFungujeHandler();
        public event KomunikaceFungujeHandler OnKomunikaceFunguje;
        public delegate void NaplneniDatKOdeslaniHandler();
        public event NaplneniDatKOdeslaniHandler OnNaplneniDatKOdeslani;
        public delegate void PrijataDataHandler();
        public event PrijataDataHandler OnPrijataData;


        /// <summary>
        /// Vytvoření instance OPC clienta se zadáním parametru rychlosti opakovaného načítání 
        /// </summary>
        /// <param name="prodlevaCteni">Čas [ms], za jak dlouho se po dokončení jednoho přečtení a zápisu opět zahájí čtení z OPC serveru. (Ovlivňuje rychlost komunikace a vytížení procesoru)</param>
        public OPCClient(int prodlevaCteni)
        {
            if ((prodlevaCteni < 0) || (prodlevaCteni > 60000))
                throw new ArgumentException("Parametr prodlevaCteni je mimo meze");

            //vytvoření a nastavení timerů, které opakovaně čtou a zapisují na OPC server, nebo kontrolují časové limity komunikace
            timerReadFromOpc = new DispatcherTimer();
            timerReadFromOpc.Interval = TimeSpan.FromMilliseconds(prodlevaCteni);
            timerReadFromOpc.Tick += cteniZeServeru;
            timerWriteToOpc = new DispatcherTimer();
            timerWriteToOpc.Interval = TimeSpan.FromMilliseconds(10);
            timerWriteToOpc.Tick += zapsatNaServer;
            timerReadTimeout = new DispatcherTimer();
            timerReadTimeout.Interval = TimeSpan.FromSeconds(10);
            timerReadTimeout.Tick += timeoutCteni;
            timerWriteTimeout = new DispatcherTimer();
            timerWriteTimeout.Interval = TimeSpan.FromSeconds(10);
            timerWriteTimeout.Tick += timeoutZapisu;
            timerRestart = new DispatcherTimer();
            timerRestart.Interval = TimeSpan.FromSeconds(1);
            timerRestart.Tick += timerRestartTick;
        }


        /// <summary>
        /// Připojení k OPC serveru. Volající musí ošetřit případnou výjimku
        /// </summary>
        /// <param name="serverUrl">Adresa OPC serveru (např "opcda://localhost/OPC.SimaticNET.DP")</param>
        public void pripojit(string serverUrl)
        {
            /*    //Odpojení od serveru
            if (server != null) 
                try
                {
                    server.Disconnect();   //Zde se to v některých případech zasekne, tak jsem to raději zrušil a při opětovném připojení si OPC server inkrementuje počet připojených klientů. Možná by to šlo přes timer...
                }
                catch
                {
                }*/

            //Připojení k serveru
            string strErr = "";
            try
            {
                strErr = "Chyba při připojování na server. ";
                Opc.URL url;
                url = new Opc.URL(serverUrl);
                OpcCom.Factory fact = new OpcCom.Factory();
                server = new Opc.Da.Server(fact, null);
                server.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
                strErr = "Chyba při vytváření groups. ";
                vytvoritGroups(server);
                strErr = "Chyba při vytváření items. ";
                vytvoritItems();
            }
            catch (Exception ex)
            {
                throw new Exception(strErr + ex.Message);
            }
        }

        /// <summary>
        /// Zahájení cyklické komunikace s OPC serverem
        /// </summary>
        public void Start()
        {
            this.run = true;
            timerReadFromOpc.Start();
            firstScanOn = true;
            writeAll = true;
        }


        /// <summary>
        /// Ukončení cyklické komunikace s OPC serverem
        /// </summary>
        public void Stop()
        {
            this.run = false;
            timerReadFromOpc.Stop();
            timerWriteToOpc.Stop();
            timerReadTimeout.Stop();
            timerWriteTimeout.Stop();
            timerRestart.Stop();
        }


        /// <summary>
        /// Automatický restart OPC klienta při chybě. (Klient se pouze znovu pokusí načíst a zapsat data, nepokouší se znovu připojit k serveru)
        /// </summary>
        private void restart()
        {
            //Zastavení timerů
            timerReadFromOpc.Stop();
            timerWriteToOpc.Stop();
            timerReadTimeout.Stop();
            timerWriteTimeout.Stop();
            writeAll = true;

            timerRestart.Start();
        }


        /// <summary>
        /// Vytvoření skupin pro čtení a zápis dat
        /// </summary>
        /// <param name="server"></param>
        private void vytvoritGroups(Opc.Da.Server server)
        {
            if (server == null)
                throw new ArgumentNullException("Parametr server není inicializovaný");
            if (!server.IsConnected)
                throw new Exception("Nelze vytvořit groups, když není připojení k serveru");

            // Vytvoření groups pro čtení a zápis
            Opc.Da.SubscriptionState groupState;
            groupState = new Opc.Da.SubscriptionState();
            groupState.Name = "Cteni";
            groupState.Active = false;
            //groupState.UpdateRate = 1000;
            groupCteni = (Opc.Da.Subscription)server.CreateSubscription(groupState);

            groupState = new Opc.Da.SubscriptionState();
            groupState.Name = "Zapis";
            groupState.Active = false;
            //groupState.UpdateRate = 1000;
            groupZapis = (Opc.Da.Subscription)server.CreateSubscription(groupState);
        }


        /// <summary>
        /// Vytvoření items (proměnných pro komunikaci s OPC serverem) a přiřazení do skupin pro čtení a zápis.
        /// </summary>
        private void vytvoritItems()
        {
            naplnitGroupCteni(groupCteni, itemsCteni);
            naplnitGroupZapis(groupZapis, itemsZapis);
        }


        /// <summary>
        /// Vypršela prodleva pro restart komunikace, pokus o start čtení
        /// </summary>
        private void timerRestartTick(object sender, EventArgs args)
        {
            timerRestart.Stop();
            timerReadFromOpc.Start();
            restarted = true;
        }


        /// <summary>
        /// Vypršel timeout komunikace pro čtení z OPC serveru
        /// </summary>
        private void timeoutCteni(object sender, EventArgs args)
        {
            timerReadTimeout.Stop();
            chybaKomunikace("Vypršel timeout pro čtení z OPC serveru.");
        }


        /// <summary>
        /// Vyprsel timeout komunikace pro zápis z OPC serveru
        /// </summary>
        private void timeoutZapisu(object sender, EventArgs args)
        {
            timerWriteTimeout.Stop();
            chybaKomunikace("Vypršel timeout pro zápis na OPC serveru.");
        }


        /// <summary>
        /// Asynchronní načtení hodnot ze serveru (odstartování timerem)
        /// </summary>
        private void cteniZeServeru(object sender, EventArgs args)
        {
            timerReadFromOpc.Stop();
            timerReadTimeout.Start();
            try
            {
                //Asynchronní čtení
                Opc.IRequest req;
                groupCteni.Read(groupCteni.Items, 123, new Opc.Da.ReadCompleteEventHandler(ReadCompleteCallback), out req);
            }
            catch (Exception ex)
            {
                chybaKomunikace("Chyba při čtení hodnot ze serveru " + ex.Message);
            }
        }


        /// <summary>
        /// Callback metoda - data byla načtena z OPC serveru
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="results"></param>
        private void ReadCompleteCallback(object clientHandle, Opc.Da.ItemValueResult[] results)
        {
            bool ok = true;
            foreach (Opc.Da.ItemValueResult readResult in results)
            {
                ok = ok && (readResult.ResultID == Opc.ResultID.S_OK) && (readResult.Quality == Opc.Da.Quality.Good);
            }
            if (ok)
            {
                try
                {
                    PretypovaniPrijatychDat(results);
                    timerReadTimeout.Stop();
                    if (this.run)
                        timerWriteToOpc.Start();
                }
                catch (Exception ex)
                {
                    chybaKomunikace(ex.Message);
                }

            }
            else
            {
                chybaKomunikace("Špatně přečtená data z OPC serveru");
            }
        }


        /// <summary>
        /// Synchronní zápis hodnot ze serveru
        /// </summary>
        public void zapsatNaServer(object sender, EventArgs args)
        {
            timerWriteToOpc.Stop();
            timerWriteTimeout.Start();
            if (OnNaplneniDatKOdeslani != null)
                OnNaplneniDatKOdeslani();   //Vyvolání události, která žádá nadřazený objekt pro naplnění dat k odeslání na OPC server 
            Thread threadZapis = new Thread(() => threadZapisWork());
            threadZapis.Start();
        }


        /// <summary>
        /// Zápis na OPC server v jiném threadu
        /// </summary>
        private void threadZapisWork()
        {
            try
            {
                List<Opc.Da.ItemValue> ItemWs = new List<Opc.Da.ItemValue>();

                foreach (Opc.Da.Item item in groupZapis.Items)
                {
                    ItemW itemW = itemsZapis[item.ItemName];
                    if ((WriteAll && (itemW.WriteMode != WMode.Assigned)) || itemW.ShouldWrite) //Výběr ItemW, které budou zapsány na OPC server
                    {
                        Opc.Da.ItemValue itemVal = new Opc.Da.ItemValue();
                        itemVal.ServerHandle = item.ServerHandle;  //Identifikace proměnné pro OPC server
                        itemVal.Value = itemW.GetWValueToClient(); //Hodnota proměnné
                        ItemWs.Add(itemVal);
                    }
                }

                if (ItemWs.Count > 0) //Test, zda je co zapisovat
                {
                    Opc.Da.ItemValue[] writeValues = ItemWs.ToArray();
                    groupZapis.Write(writeValues);
                }
                fireWriteDoneEvents();
                timerWriteTimeout.Stop();
                writeAll = false;
                if (this.run)
                {
                    restarted = false;
                    timerReadFromOpc.Start();
                }
            }
            catch (Exception ex)
            {
                writeAll = true;
                chybaKomunikace("Nepodařilo se zapsat hodnotu na OPC server. " + ex.Message);
            }
        }


        /// <summary>
        /// Přetypování přijatých dat typu Object na příslušné typy ve struktuře TPrijataData 
        /// Metoda nejdříve každou Item z ItemValueResult[] vyhledá v hashtabulce pro čtení a poté ji pomocí metody SetRValueFromClient() přetypuje na správný datový typ a uloží v datové složce Value
        /// </summary>
        private void PretypovaniPrijatychDat(Opc.Da.ItemValueResult[] results)
        {
            string name = "";
            try
            {
                foreach (Opc.Da.ItemValueResult res in results)
                {
                    name = res.ItemName;
                    if (!itemsCteni.ContainsKey(name))
                        throw new Exception("OPC Item \"" + name + "\" nebyla nalezena v hashtabulce");
                    (itemsCteni[name]).SetRValueFromClient(res.Value, res.Timestamp);   //Pomocí polymorfismun se zavolá správná metoda SetRValueFromClient() dle typu potomka Item. Např. ItemInt přetypovává na int a nádledně uloží do "Value"
                }

                if (OnPrijataData != null)
                    OnPrijataData();
                firstScanOn = false;
            }
            catch (Exception ex)
            {
                throw new Exception("Chyba při přetypovávání přijatých dat. (OPC Item \"" + name + "\") " + ex.Message);
            }
        }


        /// <summary>
        /// Reakce na to, že nastala chyba komunikace s OPC serverem (potažmo i s PLC, ale ještě to neznamená, že OPC server nekomunikuje s PLC)
        /// </summary>
        /// <param name="Message"></param>
        private void chybaKomunikace(string Message)
        {
            if (!restarted)
            {
                this.restart();  //Pokus o automatický restart komunikace
                if (OnChybaKomunikace != null)
                    OnChybaKomunikace(Message, true);
            }
            else
            {
                this.Stop();  //Restart se již nepovedl, zastavení komunikace
                if (OnChybaKomunikace != null)
                    OnChybaKomunikace(Message, false);
            }
        }


        /// <summary>
        /// Vytvoří items pro čtení v zadané group dle zadané hashtabulky
        /// </summary>
        /// <param name="group"></param>
        /// <param name="table"></param>
        protected void naplnitGroupCteni(Opc.Da.Subscription group, Dictionary<string, ItemR> table)
        {
            int i = 0;
            Opc.Da.Item[] items = new Opc.Da.Item[table.Count];
            foreach (KeyValuePair<string, ItemR> item in table)
            {
                items[i] = new Opc.Da.Item();
                items[i].ItemName = item.Key.ToString();
                i++;
            }
            items = group.AddItems(items);
            if (table.Count != group.Items.Length)  //Kontrola, zda bylo přidání items ve všech případech úspěšné
                throw new Exception(String.Format("Některé Items pro čtení se nepodařilo přidat do group \"{0}\".\n(přidáno {1} z {2}, bližší informace viz log OPC serveru)", group.Name, group.Items.Length, table.Count));
            if (table.Count == 0)
                throw new Exception(String.Format("Group \"{0}\" nemá žádné items.", group.Name));
        }


        /// <summary>
        /// Vytvoří items pro zápis v zadané group dle zadané hashtabulky
        /// </summary>
        /// <param name="group"></param>
        /// <param name="table"></param>
        protected void naplnitGroupZapis(Opc.Da.Subscription group, Dictionary<string, ItemW> table)
        {
            int i = 0;
            Opc.Da.Item[] items = new Opc.Da.Item[table.Count];
            foreach (KeyValuePair<string, ItemW> item in table)
            {
                items[i] = new Opc.Da.Item();
                items[i].ItemName = item.Key.ToString();
                i++;
            }
            items = group.AddItems(items);
            if (table.Count != group.Items.Length)  //Kontrola, zda bylo přidání items ve všech případech úspěšné
                throw new Exception(String.Format("Některé Items pro zápis se nepodařilo přidat do group \"{0}\".\n(přidáno {1} z {2}, bližší informace viz log OPC serveru)", group.Name, group.Items.Length, table.Count));
            if (table.Count == 0)
                throw new Exception(String.Format("Group \"{0}\" nemá žádné items.", group.Name));
        }


        /// <summary>
        /// Vyvolá události o úspěšném dokončení zápisu 
        /// </summary>
        private void fireWriteDoneEvents()
        {
            if (OnCommunicationCycleDone != null)
                OnCommunicationCycleDone();
            if (OnKomunikaceFunguje != null)
                OnKomunikaceFunguje();
        }



        //Implementace rozhraní IGetReadWriteCollections
        #region IGetReadWriteCollections


        /// <summary>
        /// Metoda, která vrátí ObservableCollection čtených proměnných
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<PlcVariableViewModel> GetReadVariables()
        {
            int nr = 1; //Pořadoví číslo
            ObservableCollection<PlcVariableViewModel> readVariables = new ObservableCollection<PlcVariableViewModel>();
            foreach (KeyValuePair<string, ItemR> pair in itemsCteni)
            {
                string name = String.Format("{0}.  {1}", nr, pair.Value.Name);
                PlcVariableViewModel vm = PlcVariableViewModelFactory.CreateVM(pair.Value, name);
                readVariables.Add(vm);
                nr++;
                addItemBit32ContainerBitsViewModels(pair.Value as ItemBit32RContainer, readVariables);
            }
            return readVariables;
        }


        /// <summary>
        /// Metoda, která vrátí ObservableCollection zapisovaných proměnných
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<PlcVariableViewModel> GetWriteVariables()
        {
            int nr = 1; //Pořadoví číslo
            ObservableCollection<PlcVariableViewModel> writeVariables = new ObservableCollection<PlcVariableViewModel>();
            foreach (KeyValuePair<string, ItemW> pair in itemsZapis)
            {
                string name = String.Format("{0}.  {1}", nr, pair.Value.Name);
                PlcVariableViewModel vm = PlcVariableViewModelFactory.CreateVM(pair.Value, name);
                writeVariables.Add(vm);
                nr++;
                addItemBit32ContainerBitsViewModels(pair.Value as ItemBit32WContainer, writeVariables);
            }
            return writeVariables;
        }


        /// <summary>
        /// Událost, že byl úspěšně dokončen cyklus komunikace (z toho se měří statistika)
        /// </summary>
        public event Action OnCommunicationCycleDone;


        /// <summary>
        /// ObservableCollection čtených proměnných přidá ItemBit32R z ItemBit32RContaineru
        /// </summary>
        /// <param name="container"></param>
        private void addItemBit32ContainerBitsViewModels(IBitContainer container, ObservableCollection<PlcVariableViewModel> variables)
        {
            if (container != null)
                foreach (Item itemBit in container.GetBitsArray())
                {
                    PlcVariableViewModel vm = PlcVariableViewModelFactory.CreateVM(itemBit, "\t" + itemBit.Name);
                    variables.Add(vm);
                }
        }


        #endregion IGetReadWriteCollections

    }
}
