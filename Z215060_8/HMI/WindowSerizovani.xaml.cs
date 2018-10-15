//******************************************************************************************************** 
//      VERZE 3.0
//******************************************************************************************************** 
//
//  Dialog pro seřizování stroje. Okno si ze zadaného XML souboru načte potřebné informace, dle kterých 
//    zobrazí v levé části seznam vstupů a v pravé seznam ovládacích prvků se senzory.
//
//  Popis XML souboru pro vygenerování seznamu vstupů a ovládacích prvků:
//
//  1. XML soubor, specifikovaný v konstruktoru okna, musí být validní XML soubor, tj. musí mít hlavičku 
//      s informací o verzi a kódování, uzavřené všechny tagy apod. (to se pozná nejlépe tak, že jej načte 
//      Internet Explorer).
//  2. Musí obsahovat kořenový element „Data“, ve kterém jsou vloženy všechny další elementy.
//  3. Může obsahovat elementy „Input“ (vstupů, které se zobrazují na levé straně), „Ovladani“ (Ovládání 
//      válců apod. na pravé straně) a "Separator" (Nadpist oddělující skupinu ovládání)
//  4. Každý element „Ovladani“ může obsahovat libovolný počet elementů „Senzor1“ a „Senzor2“ (zobrazení 
//      nejčastěji koncových senzorů na válcích - 1 je senzor na levé straně, 2 na pravé). 
//  5. Pořadí elementů v XML souboru určuje pořadí zobrazení prvků na obrazovce.
//  6. Element  „Input“ musí obsahovat atributy „InBit“, „Name“ a „Popis“, kde InBit je pořadové číslo 
//      bitu z PLC, který bude ovládat zobrazení hodnoty tohoto vstupu.
//  7. Element „Ovladani“ musí obsahovat atributy „OutBit1“, „OutBit2“, „Text1“, „Text2“, „Name“ a „Popis“,
//      kde OutBit je pořadové číslo bitu do PLC, kterým se bude ovládat příslušný ventil.
//  8. Atributy „InBit“ a „OutBit“ jsou typu integer, ostatní atributy jsou stringy. Stringové atributy 
//      mohou být prázdné - "".
//  9. Atributy „InBit“ a „OutBit“ musí být v celém XML souboru jedinečné, jinak bude vygenerována výjimka 
//      se zprávou, který element obsahuje duplicitní hodnotu.
//
//******************************************************************************************************** 


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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Dialog pro seřizování stroje. Okno si ze zadaného XML souboru načte potřebné informace, dle 
    /// kterých zobrazí v levé části seznam vstupů a v pravé seznam ovládacích prvků se senzory. 
    /// Pravidla pro XML soubor jsou popsána v záhlaví této třídy (WindowSerizovani.xaml.cs)
    /// </summary>
    public partial class WindowSerizovani : Window
    {
        //Kolekce pro zobrazení vstupů a ovládacích prvků v ListBoxech
        ObservableCollection<Object> ovladanis = new ObservableCollection<Object>();
        public ObservableCollection<Object> Ovladanis { get { return ovladanis; } }
        ObservableCollection<Input> inputs = new ObservableCollection<Input>();
        public ObservableCollection<Input> Inputs { get { return inputs; } }

        /// <summary>
        /// Pomocná hashtabulka senzorů a inputů pro efektivní nastavování jejich hodnoty (nemusí se procházet celá ObservableCollection při nastavení jediného DWordu)
        /// </summary>
        private Dictionary<int, Senzor> senzory = new Dictionary<int, Senzor>();
        /// <summary>
        /// Pomocná hashtabulka výstupů pro efektivní čtení jejich hodnoty (nemusí se procházet celá ObservableCollection při čtení jediného DWordu)
        /// </summary>
        private Dictionary<int, Vystup> vystupy = new Dictionary<int, Vystup>(); 

        public WindowSerizovani(string xmlTexty)
        {
            InitializeComponent();

            try
            {
                nacistZXlm(xmlTexty);
            }
            catch (Exception ex)
            {
                this.Close();
                throw (ex);
            }
        }

        #region Načtení z XML 

        /// <summary>
        /// Načtení atributů (textů, pořadových čízel bitů) pro "Ovládání" a "Inputs" z XML souboru
        /// </summary>
        /// <param name="xmlFile"></param>
        private void nacistZXlm(string xmlFile)
        {
            nacistInputs(xmlFile);
            nacistOvladani(xmlFile);
        }


        /// <summary>
        /// Načtení "Inputs" z XML souboru
        /// </summary>
        /// <param name="xmlFile"></param>
        private void nacistInputs(string xmlFile)
        {
            XDocument xmlXDoc;
            try
            {
                xmlXDoc = XDocument.Load(xmlFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Chyba při načítání textů ze souboru \"" + xmlFile + ". " + ex.Message);
            }
            var query = from c in xmlXDoc.Elements("Data").Descendants() select c;
            foreach (XElement element in query)
            {
                if (element.Name == "Input")
                    vytvoreniInputu(element, xmlFile);
            }
        }


        /// <summary>
        /// Načtení "Ovladani" z XML souboru
        /// </summary>
        /// <param name="xmlFile"></param>
        private void nacistOvladani(string xmlFile)
        {
            Ovladani ovladani = null;
            XDocument xmlXDoc;
            try
            {
                xmlXDoc = XDocument.Load(xmlFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Chyba při načítání textů ze souboru \"" + xmlFile + ". " + ex.Message);
            }
            var query = from c in xmlXDoc.Elements("Data").Descendants() select c;
            foreach (XElement element in query)
            {
                if (element.Name == "Ovladani")
                    vytvoreniOvladani(element, ref ovladani, xmlFile);
                if (element.Name == "Senzor1" | element.Name == "Senzor2")
                    vytvoreniSenzoru(element, ref ovladani, xmlFile);
                if (element.Name == "Separator")
                    vytvoreniSeparatoru(element, xmlFile);
            }
        }


        /// <summary>
        /// Vytvoření jednoho objektu Ovládání na základě načteného XML elementu
        /// </summary>
        /// <param name="OutoutElement"></param>
        private void vytvoreniOvladani(XElement element, ref Ovladani ovladani, string xmlFile)
        {    
            string errMessage = "";
            try  
            {
                errMessage = "Atribut \"OutBit1\" nebyl nalezen, nebo má špatnou hodnotu. ";  //Kontrola přítomnosti všech povinných atributů v elementu XML souboru
                int outBit1 = int.Parse(element.Attribute("OutBit1").Value);
                errMessage = "Atribut \"OutBit2\" nebyl nalezen, nebo má špatnou hodnotu. ";
                int outBit2 = int.Parse(element.Attribute("OutBit2").Value);
                errMessage = "Atribut \"Text1\" nebyl nalezen. ";
                string text1 = element.Attribute("Text1").Value;
                errMessage = "Atribut \"Text2\" nebyl nalezen. ";
                string text2 = element.Attribute("Text2").Value;
                errMessage = "Atribut \"Name\" nebyl nalezen. ";
                string name = element.Attribute("Name").Value;
                errMessage = "Atribut \"Popis\" nebyl nalezen. ";
                string popis = element.Attribute("Popis").Value;
                errMessage = "Nepodařili se vytvořit objekt \"ovladani\"";
                ovladani = new Ovladani(outBit1, outBit2, text1, text2, name, popis);  //Vytvoření ovládání
                errMessage = "U ovládání \"" + name + "\" byla nalezena duplicitní hodnota pořadového čísla výstupního bitu (Hodnoty \"outBit1\" společně s \"outBit2\" musí mít v celém XML souboru jedinečnou hodnotu)";
                vystupy.Add(outBit1, ovladani.vystup1);
                vystupy.Add(outBit2, ovladani.vystup2);
                ovladanis.Add(ovladani);
            }
            catch (Exception ex)
            {
                throw new Exception("Při načítání textů ze souboru \"" + xmlFile + "\" nastala chyba. " + "\n" + errMessage + "\n" + ex.Message);
            }
        }


        /// <summary>
        /// Vytvoření senzoru pro objekt Ovládání na základě načteného XML elementu
        /// </summary>
        /// <param name="element"></param>
        /// <param name="ovladani"></param>
        /// <param name="xmlFile"></param>
        private void vytvoreniSenzoru(XElement element, ref Ovladani ovladani, string xmlFile)
        {
            string errMessage = "";
            try  
            {
                errMessage = "Atribut \"InBit\" nebyl nalezen, nebo má špatnou hodnotu. ";  //Kontrola přítomnosti všech povinných atributů v elementu XML souboru
                int inBit = int.Parse(element.Attribute("InBit").Value);  
                errMessage = "Atribut \"Name\" nebyl nalezen. ";
                string name = element.Attribute("Name").Value;

                errMessage = "Nepodařili se vytvořit objekt \"senzor\"";
                Senzor senzor = new Senzor(inBit, name);  //Vytvoření senzoru ovládání
                if (element.Name == "Senzor1")
                    ovladani.Senzory1.Add(senzor);
                if (element.Name == "Senzor2")
                    ovladani.Senzory2.Add(senzor);

                errMessage = "U seznoru \"" + name + "\" ovládání \"" + ovladani.Nazev + "\" byla nalezena duplicitní hodnota pořadového čísla vstupního bitu (Hodnoty \"inBit1\" musí mít v celém XML souboru jedinečnou hodnotu)";
                senzory.Add(inBit, senzor);
            }
            catch (Exception ex)
            {
                throw new Exception("Při načítání textů ze souboru \"" + xmlFile + "\" nastala chyba. " + "\n" + errMessage + "\n" + ex.Message);
            }
        }


        /// <summary>
        /// Vytvoření jednoho objektu Separátor na základě načteného XML elementu
        /// </summary>
        /// <param name="OutoutElement"></param>
        private void vytvoreniSeparatoru(XElement element, string xmlFile)
        {
            string errMessage = "";
            try
            {
                errMessage = "Atribut \"Name\" nebyl nalezen. ";  //Kontrola přítomnosti všech povinných atributů v elementu XML souboru
                string name = element.Attribute("Name").Value;
                errMessage = "Atribut \"Popis\" nebyl nalezen. ";
                string popis = element.Attribute("Popis").Value;
                errMessage = "Nepodařili se vytvořit objekt \"separator\"";
                Separator separator = new Separator(name, popis);  //Vytvoření separátoru
                errMessage = "U ovládání \"" + name + "\" byla nalezena duplicitní hodnota pořadového čísla výstupního bitu (Hodnoty \"outBit1\" společně s \"outBit2\" musí mít v celém XML souboru jedinečnou hodnotu)";
                ovladanis.Add(separator);
            }
            catch (Exception ex)
            {
                throw new Exception("Při načítání textů ze souboru \"" + xmlFile + "\" nastala chyba. " + "\n" + errMessage + "\n" + ex.Message);
            }
        }


        /// <summary>
        /// Vytvoření senzoru pro objekt Ovládání na základě načteného XML elementu
        /// </summary>
        /// <param name="element"></param>
        /// <param name="ovladani"></param>
        /// <param name="xmlFile"></param>
        private void vytvoreniInputu(XElement element, string xmlFile)
        {
            string errMessage = "";
            try
            {
                errMessage = "Atribut \"InBit\" nebyl nalezen, nebo má špatnou hodnotu. ";  //Kontrola přítomnosti všech povinných atributů v elementu XML souboru
                int inBit = int.Parse(element.Attribute("InBit").Value);
                errMessage = "Atribut \"Name\" nebyl nalezen. ";
                string name = element.Attribute("Name").Value;
                errMessage = "Atribut \"Popis\" nebyl nalezen. ";
                string popis = element.Attribute("Popis").Value;

                errMessage = "Nepodařili se vytvořit objekt \"input\"";
                Input input = new Input(inBit, name, popis);  //Vytvoření senzoru ovládání
                inputs.Add(input);

                errMessage = "U Inputu \"" + name + "\" byla nalezena duplicitní hodnota pořadového čísla vstupního bitu (Hodnoty \"inBit1\" musí mít v celém XML souboru jedinečnou hodnotu)";
                senzory.Add(inBit, input);
            }
            catch (Exception ex)
            {
                throw new Exception("Při načítání textů ze souboru \"" + xmlFile + "\" nastala chyba. " + "\n" + errMessage + "\n" + ex.Message);
            }
        }


        #endregion Načtení z XML


        #region Čtení a zápis hodnot


        /// <summary>
        /// Vrátí DoubleWord po sobě jdoucích outBitů - výstupů nastavovaných tlačítky ovládání
        /// </summary>
        /// <param name="cisloDWordu">Pořadové číslo DoubleWordu. Jsou číslovány od 1</param>
        /// <returns>DoubleWord po sobě jdoucích outBitů dle aktuálnš stisknutých tlačítek ovládání</returns>
        public UInt32 GetOutDWord(int cisloDWordu)
        {
            int result = 0;
            int offset = (cisloDWordu - 1) * 32;
            for (int n = 0; n <= 31; n++)
                if (vystupy.ContainsKey(n + offset))
                    if (vystupy[n + offset].Value)
                        result = result | (1 << n);
            return (UInt32)result;
        }


        /// <summary>
        /// Nastavení hodnoty určitého DoubleWordu vstupů přijatého z PLC, nebo jiného zdroje
        /// </summary>
        /// <param name="cisloDWordu">Pořadové číslo DoubleWordu. Jsou číslovány od 1</param>
        /// <param name="value">Požadovaná hodnota DoubleWordu vstupů</param>
        public void SetIn(int cisloDWordu, UInt32 value)
        {
            int bity = (int)value;
            int offset = (cisloDWordu - 1) * 32;
            for (int n = 0; n <= 31; n++)
            {
                if (senzory.ContainsKey(n + offset))
                    senzory[n + offset].Value = (bity & 1) != 0;
                bity = bity >> 1;
            }
        }


        #endregion Čtení a zápis hodnot


        #region Buttons

        //Reakce na stisknutí buttonu výstupu levým tlačítkem myši
        private void Button1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
            ovladani.vystup1.Value = true;
            ovladani.vystup2.Value = false;
        }


        //Reakce na stisknutí buttonu výstupu levým tlačítkem myši
        private void Button2_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
            ovladani.vystup1.Value = false;
            ovladani.vystup2.Value = true;
        }


        //Reakce na uvolnění buttonu výstupu levým tlačítkem myši
        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
            ovladani.UvolnitVystupy();
        }


        //Reakce na kliknutí na 1. button
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
            ovladani.vystup1.Value = true;
            ovladani.vystup2.Value = false;
            ovladani.UvolnitVystupy();
        }


        //Reakce na kliknutí na 2. button
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
            ovladani.vystup1.Value = false;
            ovladani.vystup2.Value = true;
            ovladani.UvolnitVystupy();
        }

        #endregion Buttons


        #region Výpis použitých bitů v XML

        /// <summary>
        /// Vrátí seřazený výpis všech OutBitů načtených z XML.
        /// To se hodí jako přehled před přidáváním dalších Ovládání do rozsáhlejších XML souborů.
        /// </summary>
        /// <returns></returns>
        public string VypisOutBitu()
        {
            SortedList<int, string> vystupyView = new SortedList<int, string>();
            foreach (Object obj in Ovladanis)
            {
                Ovladani ovl = (obj as Ovladani);
                if (ovl != null)
                {
                    vystupyView.Add(ovl.vystup1.OutBit, String.Format("({0} {1} {2})", ovl.Text1, ovl.Nazev, ovl.Popis));
                    vystupyView.Add(ovl.vystup2.OutBit, String.Format("({0} {1} {2})", ovl.Text2, ovl.Nazev, ovl.Popis));
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, string> vystupView in vystupyView)
                sb.AppendLine(String.Format("{0}: {1}", vystupView.Key, vystupView.Value));
            return sb.ToString();
        }


        /// <summary>
        /// Vrátí seřazený výpis všech InBitů načtených z XML.
        /// To se hodí jako přehled před přidáváním dalších Ovládání a vstupů do rozsáhlejších XML souborů.
        /// </summary>
        /// <returns></returns>
        public string VypisInBitu()
        {
            SortedList<int, string> vstupyView = new SortedList<int, string>();
            foreach (Object obj in Ovladanis) //vstupy z kolekce ovládání
            {
                Ovladani ovl = (obj as Ovladani);
                if (ovl != null)
                {
                    foreach (Senzor sens in ovl.Senzory1)
                        vstupyView.Add(sens.InBit, String.Format("({0} - ovládání {1} {2})", sens.Nazev, ovl.Nazev, ovl.Popis));
                    foreach (Senzor sens in ovl.Senzory2)
                        vstupyView.Add(sens.InBit, String.Format("({0} - ovládání {1} {2})", sens.Nazev, ovl.Nazev, ovl.Popis));
                }
            }

            foreach (Input inp in Inputs)
                vstupyView.Add(inp.InBit, String.Format("({0} {1})", inp.Nazev, inp.Popis));


            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, string> vstupView in vstupyView)
                sb.AppendLine(String.Format("{0}: {1}", vstupView.Key, vstupView.Value));
            return sb.ToString();
        }


        #endregion Výpis použitých bitů v XML


        //Zamezení focusu listBoxů
        private void listBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;
        }


        /// <summary>
        /// Ovládání šipkami klávesnice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ovladani_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Object item = ((FrameworkElement)sender).DataContext as Object;
            if (item is Ovladani)
            {
                Ovladani ovladani = ((FrameworkElement)sender).DataContext as Ovladani;
                if (e.Key == Key.Left)
                {
                    ovladani.vystup1.Value = true;
                    ovladani.vystup2.Value = false;
                    ovladani.UvolnitVystupy();
                }
                if (e.Key == Key.Right)
                {
                    ovladani.vystup1.Value = false;
                    ovladani.vystup2.Value = true;
                    ovladani.UvolnitVystupy();
                }
            }           
        }
        

    }


    #region Pomocné třídy

    /// <summary>
    ///  Výstup. 
    ///  (Implementuje rozhraní INotifyPropertyChanged pro vlastnost Value, aby se o jeho změně dozvěděla ObservableCollection)
    /// </summary>
    public class Ovladani : INotifyPropertyChanged
    {
        public Vystup vystup1 { get; set; } //Výstup, který se bude ovládat 1. tlačítkem (levým)
        public Vystup vystup2 { get; set; } //Výstup, který se bude ovládat 2. tlačítkem (pravým)
        public string Text1 { get; set; }  //Nápis na 1. tlačítku (levém)
        public string Text2 { get; set; }  //Nápis na 2. tlačítku (levém)
        public string Nazev { get; set; }  //Název, který se zobrazí tučně (např "V1")
        public string Popis { get; set; }  //Popis, který se zobrazí za zázvem (např " - válec přesunu prázdného plata")
        public ObservableCollection<Senzor> Senzory1 { get; set; } //Kolekce senzorů (vlevo) 
        public ObservableCollection<Senzor> Senzory2 { get; set; } //Kolekce senzorů (vpravo) 
        private DispatcherTimer nullTimer;

        public event PropertyChangedEventHandler PropertyChanged;


        public Ovladani(int outBit1, int outBit2, string text1, string text2, string nazev, string popis)
        {
            vystup1 = new Vystup(outBit1);
            vystup1.OnNasetovan += new Vystup.NasetovanHandler(vystup1_OnNasetovan);
            vystup2 = new Vystup(outBit2);
            vystup2.OnNasetovan += new Vystup.NasetovanHandler(vystup2_OnNasetovan);
            this.Text1 = text1;
            this.Text2 = text2;
            this.Nazev = nazev;
            this.Popis = popis;

            Senzory1 = new ObservableCollection<Senzor>();
            Senzory2 = new ObservableCollection<Senzor>();

            nullTimer = new DispatcherTimer();
            nullTimer.Interval = TimeSpan.FromMilliseconds(1000);
            nullTimer.Tick += new EventHandler(nullTimer_Tick);
        }


        //Reakce na nasetování 1. výstupu
        void vystup1_OnNasetovan()
        {
            vystup2.Value = false;
            nullTimer.Stop();
        }


        //Reakce na nasetování 2. výstupu
        void vystup2_OnNasetovan()
        {
            vystup1.Value = false;
            nullTimer.Stop();
        }


        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        //nastaví null do hodnoty value po definované době 
        void nullTimer_Tick(object sender, EventArgs e)
        {
            nullTimer.Stop();
            vystup1.Value = false;
            vystup2.Value = false;
        }


        /// <summary>
        /// Reakce na uvolnění výstupů
        /// </summary>
        public void UvolnitVystupy()
        {
            nullTimer.Stop();
            nullTimer.Start();
        }
    }


    /// <summary>
    /// Výstup v podstatě pouze zabaluje bool hodnotu výstupu ovládání, aby byla referenční. 
    /// Tj. aby se na něj dalo odkazovat z hashtabulky.
    /// </summary>
    public class Vystup
    {
        public int OutBit { get; set; }  //Číslo výstupního bitu, který bude ovládaný tlačítkem 

        private bool value;
        /// <summary>
        /// Hodnota výstupu
        /// </summary>
        public bool Value 
        {
            get { return this.value; }
            set
            {
                this.value = value;
                if (value)
                    if (OnNasetovan != null)
                        OnNasetovan();   
            }
        }      

        public delegate void NasetovanHandler();
        public event NasetovanHandler OnNasetovan;

        public Vystup(int outBit)
        {
            Value = false;
            this.OutBit = outBit; 
        }
    }


    /// <summary>
    ///  Senzor na ovládacím prvku. 
    ///  (Implementuje rozhraní INotifyPropertyChanged pro vlastnost Value, aby se o jeho změně dozvěděla ObservableCollection)
    /// </summary>
    public class Senzor : INotifyPropertyChanged
    {
        public int InBit { get; set; }  //Číslo vstupního bitu, podle kterého se bude ovládat hodnota Value
        public string Nazev { get; set; }  //Název, který se zobrazí na grafockém zobrazení senzoru (např "IN")
        private bool value;

        public event PropertyChangedEventHandler PropertyChanged;

        public Senzor(int inBit, string nazev)
        {
            this.InBit = inBit;
            this.Nazev = nazev;   
        }

        public bool Value
        {
            get { return value; }
            set
            {
                bool zmena = this.value != value;
                if (zmena)
                {
                    this.value = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }


    /// <summary>
    ///  Vstup pro zobrazení v listdoxu vstupů. Je odvozen z třídy Senzor, kterou tozšiřuje o datovou složku popis
    /// </summary>
    public class Input : Senzor
    {
        public string Popis { get; set; }  //Popis, který se zobrazí na pravo od vizualizace hodnoty (např "Optická závora")

        public Input(int inBit, string nazev, string popis)
            : base(inBit, nazev)
        {
            this.Popis = popis;
        }
    }


    /// <summary>
    ///  Separátor ovládacích prvků. Zobrazí se jako nadpis pro určitou skupinu ovládání
    /// </summary>
    public class Separator
    {
        public string Nazev { get; set; }  //Název, který se zobrazí tučně
        public string Popis { get; set; }  //Popis, který se zobrazí na pravo od názvu

        public Separator(string nazev, string popis)
        {
            if (nazev != "")
                this.Nazev = nazev;      
            this.Popis = popis;
        }
    }


    /// <summary>
    /// Třída určená k rozlišení, jakého typu je objekt v kolekci Ovladanis (podle toho se zobrazí jako ovlásání, nebo jako separátor (nadpis))
    /// </summary>
    public class OvladaniTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OvladaniDataTemplate { get; set; }
        public DataTemplate SeparatorDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
           // Output vystup = (Output)item;
            if (item is Ovladani)
                return OvladaniDataTemplate;
            else
                return SeparatorDataTemplate;
        }
    }


    #endregion Pomocné třídy

}
