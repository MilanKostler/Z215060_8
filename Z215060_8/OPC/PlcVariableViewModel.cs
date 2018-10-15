//****************************************************************************
//      VERZE 2.0 - Interface IGetReadWriteCollections
//      VERZE 1.1 - Přidání typu PlcInt16VariableViewModel
//      VERZE 1.0
//****************************************************************************
// 
//  Třídy viewModelu pro zobrazení aktuálnách dat komunikovaných s PLC pro
//      účely ladění.
//
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace HMI.Debug
{
    /// <summary>
    /// Delegát pro událost, že se změnila hodnota PLC proměnné
    /// </summary>
    /// <param name="varValue">Aktuální hodnota nové proměnné</param>
    public delegate void ChangeValueHandler(object varValue);



    /// <summary>
    /// Rozhraní zajišťujíví, že PLC proměnné při své změně vyvolá událost, v jejíž parametru pošle svou novou aktuální hodnotu
    /// </summary>
    public interface IOnVariableChanged
    {
        /// <summary>
        /// Událost, že došlo ke změně hodnoty PLC proměnné. varValue je nová hodnota proměnné.
        /// </summary>
        event ChangeValueHandler OnChangeValue;


        /// <summary>
        /// Vrátí hodnotu Value jako objekt
        /// </summary>
        /// <returns></returns>
        object GetValue();
    }



    /// <summary>
    /// Rozhranní zajišťující, že komunikační třída, cyklicky čtoucí a zapisující proměnné poskytuje ObservableCollection těchto proměnných
    /// a událost, že byl dokončen komunikační cyklus.
    /// </summary>
    public interface IGetReadWriteCollections
    {
        /// <summary>
        /// Metoda, která vrátí ObservableCollection čtených proměnných
        /// </summary>
        /// <returns></returns>
        ObservableCollection<PlcVariableViewModel> GetReadVariables();


        /// <summary>
        /// Metoda, která vrátí ObservableCollection zapisovaných proměnných
        /// </summary>
        /// <returns></returns>
        ObservableCollection<PlcVariableViewModel> GetWriteVariables();


        /// <summary>
        /// Událost, že byl úspěšně dokončen cyklus komunikace (z toho se měří statistika)
        /// </summary>
        event Action OnCommunicationCycleDone;
    }



    /// <summary>
    /// Továrna na vytváření objektů PlcVariableViewModel dle zadaných parametrů
    /// </summary>
    public static class PlcVariableViewModelFactory
    {
        public static PlcVariableViewModel CreateVM(IOnVariableChanged varChangeObj, string varName)
        {
            object obj = varChangeObj.GetValue();
            PlcVariableViewModel pvvm = null;
            if (obj is bool)
                pvvm = new PlcBoolVariableViewModel(varChangeObj, varName);
            if (obj is Int16)
                pvvm = new PlcInt16VariableViewModel(varChangeObj, varName);
            if (obj is Byte)
                pvvm = new PlcByteVariableViewModel(varChangeObj, varName);
            if (obj is UInt16)
                pvvm = new PlcUInt16VariableViewModel(varChangeObj, varName);
            if (obj is UInt32)
                pvvm = new PlcUInt32VariableViewModel(varChangeObj, varName);
            if (obj is string)
                pvvm = new PlcStringVariableViewModel(varChangeObj, varName);
            if (pvvm == null)
                pvvm = new PlcVariableViewModel(varChangeObj, varName);
            return pvvm;
        }

    }



    /// <summary>
    /// ViewModel PLC proměnných
    /// </summary>
    public class PlcVariablesViewModel : ViewModelBase
    {
        private StatistikaKomunikace statistika = new StatistikaKomunikace();
        /// <summary>
        /// Sleduje statistické údaje o komunikaci
        /// </summary>
        public StatistikaKomunikace Statistika
        {
            get { return statistika; }
        }


        /// <summary>
        /// Kolekce všech ViewModelů PLC proměnných pro čtení
        /// </summary>
        public ObservableCollection<PlcVariableViewModel> ReadVariables = new ObservableCollection<PlcVariableViewModel>();

        /// <summary>
        /// Kolekce všech ViewModelů PLC proměnných pro Zápis
        /// </summary>
        public ObservableCollection<PlcVariableViewModel> WriteVariables = new ObservableCollection<PlcVariableViewModel>();


        /// <summary>
        /// Konstruktor
        /// </summary>
        public PlcVariablesViewModel(IGetReadWriteCollections communicationClient)
        {
            ReadVariables = communicationClient.GetReadVariables();
            WriteVariables = communicationClient.GetWriteVariables();
            communicationClient.OnCommunicationCycleDone += () => { Statistika.Zmerit(); };

            Statistika.OnDataReady += () => NotifyPropertyChanged("Statistika");
        }
    }



    //ViewModely jednotlivých typů proměnných
    #region Typy proměnných


    /// <summary>
    /// ViewModel PLC proměnné
    /// </summary>
    public class PlcVariableViewModel : ViewModelBase
    {
        private string nazev;
        /// <summary>
        /// Název či popis PLC proměnné
        /// </summary>
        public string Nazev
        {
            get { return nazev; }
            set
            {
                if (nazev != value)
                {
                    nazev = value;
                    NotifyPropertyChanged("Nazev");
                }
            }
        }


        private string hodnota = null;
        /// <summary>
        /// Hodnota PLC proměnné
        /// </summary>
        public string Hodnota
        {
            get { return hodnota; }
            set
            {
                if (hodnota != value)
                {
                    hodnota = value;
                    NotifyPropertyChanged("Hodnota");
                }
            }
        }


        /// <summary>
        /// Zpětná reference na PLC proměnnou
        /// </summary>
        protected IOnVariableChanged plcVarRef;


        protected SolidColorBrush barva = Brushes.Peru;
        /// <summary>
        /// Barva kategorie
        /// </summary>
        public SolidColorBrush Barva
        {
            get
            {
                return barva;
            }
        }


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="varChangeObj"></param>
        /// <param name="varName"></param>
        public PlcVariableViewModel(IOnVariableChanged varObj, string varName)
        {
            this.Nazev = varName;
            this.plcVarRef = varObj;
            plcVarRef.OnChangeValue += (object varValue) => { CastValue(); }; //smazat (object varValue)
            CastValue();
        }


        /// <summary>
        /// Převede hodnotu PLC proměnné z typu object na string a zapíše jí do property Hodnota
        /// </summary>
        /// <param name="varValue"></param>
        public virtual void CastValue()
        {
            object varValue = plcVarRef.GetValue();
            Hodnota = varValue.ToString();
        }


        /// <summary>
        /// Reakce na klik myší
        /// </summary>
        public virtual void MouseDownHandler()
        {
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu bool
    /// </summary>
    class PlcBoolVariableViewModel : PlcVariableViewModel
    {
        public PlcBoolVariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName)
        {
            barva = Brushes.LightBlue;
            string pokus = varObj.GetValue().ToString();
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu Int16
    /// </summary>
    class PlcInt16VariableViewModel : PlcVariableViewModel
    {
        public PlcInt16VariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName)
        {
            barva = Brushes.DarkSeaGreen;
            string pokus = varObj.GetValue().ToString();
        }
    }



    /// <summary>
    /// Bázová třída pro ViewModel PLC proměnné typu UInt (Byte, UInt16, UInt32)
    /// </summary>
    abstract class PlcUIntVariableViewModel : PlcVariableViewModel
    {
        /// <summary>
        /// Formátovací řetězec pro zobrazení v hexadecimálním tvaru
        /// </summary>
        protected string hexFormat = "0x{0:X}";

        /// <summary>
        /// Zda se má hodnota zobrazovat hexadecimálně
        /// </summary>
        public bool Hexa { get; set; }


        public PlcUIntVariableViewModel(IOnVariableChanged varObj, string varName, string hexFormat)
            : base(varObj, varName)
        {
            barva = Brushes.LightGreen;
            this.hexFormat = hexFormat;
            Hexa = true;
            CastValue();
        }


        /// <summary>
        /// Reakce na klik myší
        /// </summary>
        public override void MouseDownHandler()
        {
            Hexa = !Hexa;
            CastValue();
        }


        /// <summary>
        /// Převede hodnotu PLC proměnné z integeru na string a zapíše jí do property Hodnota
        /// </summary>
        /// <param name="varValue"></param>
        public override void CastValue()
        {
            object varValue = plcVarRef.GetValue();
            if (Hexa)
                Hodnota = String.Format(hexFormat, varValue);
            else
                Hodnota = varValue.ToString();
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu Byte
    /// </summary>
    class PlcByteVariableViewModel : PlcUIntVariableViewModel
    {
        public PlcByteVariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName, "0x{0:X2}")
        {
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu UInt16
    /// </summary>
    class PlcUInt16VariableViewModel : PlcUIntVariableViewModel
    {
        public PlcUInt16VariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName, "0x{0:X4}")
        {
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu UInt32
    /// </summary>
    class PlcUInt32VariableViewModel : PlcUIntVariableViewModel
    {
        public PlcUInt32VariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName, "0x{0:X8}")
        {
        }
    }



    /// <summary>
    /// ViewModel PLC proměnné typu string
    /// </summary>
    class PlcStringVariableViewModel : PlcVariableViewModel
    {
        /// <summary>
        /// Zda se má hodnota zobrazovat hexadecimálně
        /// </summary>
        public bool Hexa { get; set; }


        public PlcStringVariableViewModel(IOnVariableChanged varObj, string varName)
            : base(varObj, varName)
        {
            barva = Brushes.LemonChiffon;
            CastValue();
        }


        /// <summary>
        /// Reakce na klik myší
        /// </summary>
        public override void MouseDownHandler()
        {
            Hexa = !Hexa;
            CastValue();
        }


        /// <summary>
        /// Převede hodnotu PLC proměnné z integeru na string a zapíše jí do property Hodnota
        /// </summary>
        /// <param name="varValue"></param>
        public override void CastValue()
        {
            object varValue = plcVarRef.GetValue();
            if (Hexa)
            {
                string val = (string)varValue;
                byte[] bytes = Encoding.ASCII.GetBytes(val);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < val.Length; i++)
                {
                    sb.Append(String.Format("'"));
                    sb.Append(Encoding.ASCII.GetString(new[] { bytes[i] }));
                    sb.Append(String.Format("' (0x{0:X2})", bytes[i]));
                    if (i < (val.Length - 1))
                        sb.Append("\n");
                }
                Hodnota = sb.ToString();

            }
            else
                Hodnota = varValue.ToString();
        }
    }


    #endregion Typy proměnných
}
