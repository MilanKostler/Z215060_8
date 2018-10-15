//******************************************************************************************************** 
//      VERZE 2.0
//******************************************************************************************************** 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;
using System.Windows.Media;

namespace HMI
{
    /// <summary>
    /// Třída pro ošéfování všeho okolo alarmů.
    /// - Načtení textů alarmů z XML
    /// - Přijímání informací o aktuálních alarmech na stroji
    /// - Vytváření událostí, že alarm nastal, nebo byl potvrzen
    /// - Poskytování kolekce aktivních alarmů grafivkému prostředí aplikace
    /// </summary>
    public class Alarms: INotifyPropertyChanged
    {
        /// <summary>
        /// Data defaultní kategorie (pokud při načítání z xml nebyla nalezena kategorie daného čísla)
        /// </summary>
        private KategorieAlarmu defaultniKategorie = new KategorieAlarmu() { Cislo = 0, Barva = System.Windows.Media.Brushes.Black, Nazev = "Alarm", SortPriority = 10};  //Defaultní kategorie alarmu (pokud není explicitně zadaná v XML souboru)

        private ObservableCollection<Alarm> aktivniAlarmy = new ObservableCollection<Alarm>();
        /// <summary>
        /// Kolekce všech aktivních alarmů pro zobrazení na GUI.
        /// </summary>
        public ObservableCollection<Alarm> AktivniAlarmy { get { return aktivniAlarmy; } }

        /// <summary>
        /// Pomocná hashtabulka všech aktivních alarmů, pro účely zjištění, zda je alarm dle jeho čísla aktivní (aby se nemuselo procházet celým polem seznamAlarmu)
        /// </summary>
        private Dictionary<int, Alarm> hashAktivniAlarmy = new Dictionary<int, Alarm>();

        /// <summary>
        /// Tabulka všech alarmů načtených z xml souboru
        /// </summary>
        private Alarm[] seznamAlarmu = new Alarm[0];

        private bool jeAlarm;
        /// <summary>
        /// Je aktivní alespoň jeden alarm
        /// </summary>
        public bool JeAlarm
        {
            get { return jeAlarm; }
            set 
            {
                if (jeAlarm != value)
                {
                    jeAlarm = value; 
                    NotifyPropertyChanged("JeAlarm");
                }
            }
        }


        //deklarace událostí
        public delegate void NahozenAlarmHandler(Alarm alarm);
        /// <summary>
        /// Nastane při vzniku alarmu (nahození alarmu strojem)
        /// </summary>
        public event NahozenAlarmHandler OnNahozenAlarm;
        
        public delegate void PotvrzenAlarmHandler(Alarm alarm, TimeSpan dobaTrvani);
        /// <summary>
        /// Nastane při potvrzení alarmu
        /// </summary>
        public event PotvrzenAlarmHandler OnPotvrzenAlarm;

        //Byla změněna vlastnost
        public event PropertyChangedEventHandler PropertyChanged;


        //Implementace rozhraní (aby nadřazený prvek věděl, že se změnila nějaká vlastnost)
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        /// <summary>
        /// Vrátí referenci na alarmu v poli seznamAlarmu dle jeho čísla. 
        /// Pokud v poli není alarm daného čísla, pokusí se jej vyhledat v hashtabulce aktivních alarmů.
        /// Pokud není ani tam, vytvoří alarm nový, s automatickým textem
        /// </summary>
        /// <param name="cisloAlarmu"></param>
        /// <returns></returns>
        private Alarm getAlarm(int cisloAlarmu)
        {
            Alarm alarm;
            if ((cisloAlarmu > 0) && (cisloAlarmu <= seznamAlarmu.Count())) //Zda je alarm ze seznamu načteného z XML
            {
                alarm = seznamAlarmu[cisloAlarmu - 1];  // Alarmy jsou zde interně číslovány od nuly.
            }
            else
            {
                if (hashAktivniAlarmy.ContainsKey(cisloAlarmu))  //Zda je alarm v hashtabulce aktivních alarmů
                    alarm = hashAktivniAlarmy[cisloAlarmu];
                else
                    alarm = new Alarm(cisloAlarmu, String.Format("Alarm číslo {0}", cisloAlarmu), defaultniKategorie);
            }

            return alarm;
        }


        /// <summary>
        /// Aktivace/deaktivace alarmu dle jeho čísla.
        /// </summary>
        /// <param name="cisloAlarmu">Číslo alarmu (číslování od 1)</param>
        /// <param name="aktivni">Nahození, či shození alarmu</param>
        public void SetAlarm(int cisloAlarmu, bool aktivni)
        {
            Alarm alarm = getAlarm(cisloAlarmu);
            bool zmena = alarm.Aktivni ^ aktivni;
            if (!zmena)
                return;  //Není co měnit

            if (aktivni)
            {
                pridatDoAktivnichAlarmu(cisloAlarmu, alarm);
                if (OnNahozenAlarm != null)  //Vyvolání události nahození alarmu
                    OnNahozenAlarm(alarm);
            }
            else
            {
                if (OnPotvrzenAlarm != null)  //Vyvolání události potvrzení alarmu
                {
                    TimeSpan ts = DateTime.Now - alarm.CasVzniku;
                    OnPotvrzenAlarm(alarm, ts);
                }
                odebratZAktivnichAlarmu(cisloAlarmu);  //Odebrání z kolekce aktivních alarmů
            }
            JeAlarm = hashAktivniAlarmy.Count != 0;
        }


        /// <summary>
        /// Přidá alarm do kolekcí aktivních alarmů (AktivniAlarmy a hashAktivniAlarmy) dle čísla alarmu a nastaví mu, že je aktivní
        /// </summary>
        /// <param name="cisloAlarmu"></param>
        /// <param name="alarm"></param>
        private void pridatDoAktivnichAlarmu(int cisloAlarmu, Alarm alarm)
        {
            alarm.Aktivni = true;  //Nahození alarmu
            //Přidání do kolekce aktivních alarmů
            hashAktivniAlarmy.Add(cisloAlarmu, alarm);
            int indexView = 0;
            while ((indexView < AktivniAlarmy.Count) && (alarm.Kategorie.SortPriority < AktivniAlarmy[indexView].Kategorie.SortPriority))
                indexView++;
            AktivniAlarmy.Insert(indexView, alarm);
        }


        /// <summary>
        /// Odebere alarm z kolekcí aktivních alarmů (AktivniAlarmy a hashAktivniAlarmy) dle čísla alarmu a nastaví mu, že není aktivní
        /// </summary>
        /// <param name="cisloAlarmu"></param>
        private void odebratZAktivnichAlarmu(int cisloAlarmu)
        {
            int pocet = AktivniAlarmy.Count - 1;
            for (int i = pocet; i >= 0; i--)
            {
                if (AktivniAlarmy[i].CisloAlarmu == cisloAlarmu)
                {
                    AktivniAlarmy[i].Aktivni = false;
                    AktivniAlarmy.RemoveAt(i);
                }
            }
            hashAktivniAlarmy.Remove(cisloAlarmu);
        }


        /// <summary>
        /// Aktivace/deaktivace alarmů po bytech
        /// </summary>
        /// <param name="cisloBytu">Kolikátý byte alarmů změnit</param>
        /// <param name="value">Osmice alarmů - 1bit = jeden alarm</param>
        public void SetByteAlarmu(int cisloBytu, byte value)
        {
            int start = ((cisloBytu - 1) * 8) + 1;
            SetIntAlarmu(start, value, 8);
        }


        /// <summary>
        /// Aktivace/deaktivace alarmů po wordech
        /// </summary>
        /// <param name="cisloBytu">Kolikátý Word alarmů změnit</param>
        /// <param name="value">šestnáct alarmů - 1bit = jeden alarm</param>
        public void SetWordAlarmu(int cisloWordu, UInt16 value)
        {
            int start = ((cisloWordu - 1) * 16) + 1;
            SetIntAlarmu(start, value, 16);
        }


        /// <summary>
        /// Aktivace/deaktivace alarmů po doublewordech
        /// </summary>
        /// <param name="cisloBytu">Kolikátý DWord alarmů změnit</param>
        /// <param name="value">32 alarmů - 1bit = jeden alarm</param>
        public void SetDWordAlarmu(int cisloDWordu, UInt32 value)
        {
            int start = ((cisloDWordu - 1) * 32) + 1;
            SetIntAlarmu(start, value, 32);
        }


        /// <summary>
        /// Aktivace/deaktivace alarmů po wordech
        /// </summary>
        /// <param name="cisloBytu">Kolikátým alarmem začít</param>
        /// <param name="value">Osmice alarmů - 1bit = jeden alarm</param>
        private void SetIntAlarmu(int start, UInt32 uInt32bitu, int pocet)
        {
            bool aktivni;
            for (int i = 0; i < pocet; i++)
            {
                aktivni = (uInt32bitu & 1) != 0;
                uInt32bitu = uInt32bitu >> 1;
                SetAlarm(start + i, aktivni);
            }
        }


        /// <summary>
        /// Načtení textů alarmů ze zadaného xml souboru. Nutno ošetžit případnou vyjímku
        /// </summary>
        /// <param name="xmlFile"></param>
        public void NacistAlarmyZXlm(string xmlFile)
        {
            string strRootElement = "Data";
            string strNazevAtributu = "Text";
            string strElementName = "Alarm";
            XDocument xmlXDoc = XDocument.Load(xmlFile);
            Dictionary<int, KategorieAlarmu> vsechnyKategorie = NacistKategorieZXlm(xmlXDoc, strRootElement);
            var query = from c in xmlXDoc.Elements(strRootElement).Descendants("Alarmy").Descendants() select c;  //xmlXDoc.Elements(StrRootElement) 
            int i = 1;
            bool chyba = false;
            seznamAlarmu = new Alarm[query.Count()];

            try
            {
                foreach (XElement alarmElement in query)
                {
                    Alarm alarm;

                    int? cisloKatogorie = (int?)alarmElement.Attribute("Kategorie");
                    if (cisloKatogorie == null)
                        cisloKatogorie = 1;
                    KategorieAlarmu kategorie = defaultniKategorie;
                    kategorie.Cislo = cisloKatogorie.Value;
                    if (vsechnyKategorie.ContainsKey(kategorie.Cislo))
                        kategorie = vsechnyKategorie[kategorie.Cislo];

                    if (alarmElement.Name == (strElementName + i.ToString()))
                    {
                        alarm = new Alarm(i, alarmElement.Attribute(strNazevAtributu).Value, kategorie);
                        seznamAlarmu[i - 1] = alarm;
                    }
                    else
                        chyba = true;
                    i++;
                }
                if (chyba)
                    throw new Exception("Chybný název elementu alarmu nebo špatné pořadí.");
            }
            catch (Exception ex)
            {
                seznamAlarmu = new Alarm[0];  //Zrušení celého načtení alarmů
                throw (ex);
            }
        }


        /// <summary>
        /// Načte kategorie alarmů z XML dokumentu do kolekce, nebo vyhodí vyjímku.
        /// </summary>
        /// <param name="xmlXDoc"></param>
        /// <returns></returns>
        private Dictionary<int, KategorieAlarmu> NacistKategorieZXlm(XDocument xmlXDoc, string strRootElement)
        {
            Dictionary<int, KategorieAlarmu> vsechnyKategorie = new Dictionary<int, KategorieAlarmu>();
            try
            {
                var query = from c in xmlXDoc.Elements(strRootElement).Elements("KategorieAlarmu") select c;   

                foreach (XElement book in query)
                {
                    KategorieAlarmu kat = new KategorieAlarmu();

                    int? cislo = (int?)book.Attribute("Kategorie");
                    string nazev = (string)book.Attribute("Nazev");
                    string strBarva = (string)book.Attribute("Barva");
                    int? sortPriority = (int?)book.Attribute("SortPriority");

                    if (cislo == null)
                        throw new Exception("U \"KategorieAlarmu\" se nepodařilo načíst attribut \"Kategorie\"");
                    if (nazev == null)
                        throw new Exception(String.Format("U \"KategorieAlarmu\" Kategorie={0} se nepodařilo načíst attribut \"Nazev\"", cislo.Value));
                    if (strBarva == null)
                        throw new Exception(String.Format("U \"KategorieAlarmu\" Kategorie={0} se nepodařilo načíst attribut \"Barva\"", cislo.Value));
                    if (sortPriority == null)
                        throw new Exception(String.Format("U \"KategorieAlarmu\" Kategorie={0} se nepodařilo načíst attribut \"SortPriority\"", cislo.Value));

                    kat.Cislo = cislo.Value;
                    kat.Nazev = nazev;
                    kat.SortPriority = sortPriority.Value;
                    try //parsování barvy
                    {
                        Color col = (Color)ColorConverter.ConvertFromString(strBarva);
                        kat.Barva = new SolidColorBrush(col);
                    }
                    catch
                    {
                        throw new Exception(String.Format("U \"KategorieAlarmu\" Kategorie={0} je špatný formát attributu \"Barva\"", cislo.Value));
                    }

                    try //Vložení kategorie do hash tabulky
                    {
                        vsechnyKategorie.Add(kat.Cislo, kat);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("U \"KategorieAlarmu\" Kategorie={0} je špatné číslo kategorie ({1}).", cislo.Value, ex.Message));
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Chyba při načítání kategorií alarmů: " + ex.Message);
            }

            return vsechnyKategorie;
        }
    }


    /// <summary>
    /// Třída reprezentující alarm.
    /// </summary>
    public class Alarm
    {
        /// <summary>
        /// Text chyby, který se zobrazí obsluze
        /// </summary>
        public string Hlaska { get; set; }

        private DateTime casVzniku;
        /// <summary>
        /// Čas vzniku alarmu
        /// </summary>
        public DateTime CasVzniku
        {
            get { return casVzniku; }
        }

        private int cisloAlarmu;
        /// <summary>
        /// Číslo alarmu (číslováno od 1 tak, jak je to načteno z XML souboru)
        /// </summary>
        public int CisloAlarmu
        {
            get { return cisloAlarmu; }
        }

        private bool aktivni;
        /// <summary>
        /// Zda je alatm aktivní (právě nahozený strojem a nepotvrzený obsluhou)
        /// </summary>
        public bool Aktivni
        {
            get { return aktivni; }
            set 
            {
                if (aktivni != value)
                {
                    aktivni = value;
                    if (value)
                        casVzniku = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Kategorie alarmu pro možnosti rozdělení na Errors, Warnings apod.
        /// V ObservableCollection AktivniAlarmy jsou pak vždy alarmy s nižší kategorií řazeny nad alarmy s vyšší kategorií.
        /// </summary>
        public KategorieAlarmu Kategorie { get; set; }

        //Text tooltipu
        public string ToolTip
        {
            get
            {
                return string.Format("Alarm č. {0} ({1})", CisloAlarmu, Kategorie.Nazev);
            }
        }

        //Konstruktor
        public Alarm(int cisloAlarmu, string hlaska, KategorieAlarmu kategorie)
        {
            casVzniku = DateTime.Now;
            this.cisloAlarmu = cisloAlarmu;
            Hlaska = hlaska;
            Kategorie = kategorie;
        }
    }




    /// <summary>
    /// Struktura kategorie alarmu. Obsahuje číslo kategorie alarmu, barvu, prioritu řazení apod.
    /// </summary>
    public struct KategorieAlarmu
    {
        /// <summary>
        /// Název kategorie (např. Porucha, Varování apod...)
        /// </summary>
        public string Nazev { get; set; }

        /// <summary>
        /// Číslo kategorie alarmu.
        /// </summary>
        public int Cislo { get; set; }


        /// <summary>
        /// Barva kategorie
        /// </summary>
        public System.Windows.Media.SolidColorBrush Barva { get; set; }


        /// <summary>
        /// Priorita zobrazení aktivních alarmů. Čím vyšší priorita, tím se alarm mezi aktivními alarmy zobrazí výš. 
        /// Alarmy se stejnou prioritou jsou pak řazeny od nejnovějšího po nejstarší.
        /// </summary>
        public int SortPriority { get; set; }
    }
}
