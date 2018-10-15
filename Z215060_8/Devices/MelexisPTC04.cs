using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using PSF090367AAMLXModule;
using MLXMPTCommon;
using System.Threading;
using System.Windows.Threading;
//using PTC04PSFModule;
//using CommUnit;

namespace Z215060_8.Melexis
{
    /// <summary>
    /// Měřicí přístroj Melexis TTC-04
    /// </summary>
    public class MelexisPTC04 : INotifyPropertyChanged
    {
        #region Properties


        private bool meritIdd;
        /// <summary>
        /// Zda má probíhat kontinuální měření Idd
        /// </summary>
        public bool MeritIdd
        {
            get { return meritIdd; }
            set 
            {
                if (meritIdd != value)
                {
                    bool start = (!ProbihaMereni && value);
                    meritIdd = value;

                    if (!meritIdd)
                        valuesToNull(true, false);
                    if (start)
                        startMereni();
                    NotifyPropertyChanged("MeritIdd");
                }
            }
        }


        private bool meritOut;
        //Zda má probíhat kontinuální měření Out
        public bool MeritOut
        {
            get { return meritOut; }
            set
            {
                if (meritOut != value)
                {
                    bool start = (!ProbihaMereni && value);
                    meritOut = value;
                    if (!meritOut)
                        valuesToNull(false, true);
                    if (start)
                        startMereni();
                    NotifyPropertyChanged("MeritOut");
                }
            }
        }


        /// <summary>
        /// Probéhá měření
        /// </summary>
        public bool ProbihaMereni
        {
            get { return meritIdd || meritOut; }
        }


        private int kanal = 1;
        /// <summary>
        /// Kanál, ze kterého se má měřit (1..DieA, 2..DieB)
        /// </summary>
        public int Kanal
        {
            get { return kanal; }
            set 
            {
                if ((value >= 1) && (value <= 2))
                    kanal = value;
                else
                    throw new ArgumentException("Hondota property \"Kanal\" objektu třídy MelexisPTC04 musí být v rozmezí 1 - 2.");
            }
        }


        private bool connected;
        /// <summary>
        /// Zařízení je připojeno přes RS232
        /// </summary>
        public bool Connected
        {
            get { return connected; }
        }


        /// <summary>
        /// Zařízení není připojeno přes RS232
        /// </summary>
        public bool Disconnected
        {
            get { return !connected; }
        }


        private MeasuredValue iddA = new MeasuredValue();
        /// <summary>
        /// Hotnota IDD [mA] z kanálu A
        /// </summary>
        public MeasuredValue IddA
        {
            get { return iddA; }
        }


        private MeasuredValue iddB = new MeasuredValue();
        /// <summary>
        /// Hotnota IDD [mA] z kanálu B
        /// </summary>
        public MeasuredValue IddB
        {
            get { return iddB; }
        }


        private MeasuredValue outA = new MeasuredValue();
        /// <summary>
        /// Hotnota Out [lsb] z kanálu A
        /// </summary>
        public MeasuredValue OutA
        {
            get { return outA; }
        }


        private MeasuredValue outB = new MeasuredValue();
        /// <summary>
        /// Hotnota Out [lsb] z kanálu B
        /// </summary>
        public MeasuredValue OutB
        {
            get { return outB; }
        }


        #endregion Properties


        /// <summary>
        /// Objekt importovaný z DLL knihovny, reprezentující fyzické zařízení
        /// </summary>
        private PSF090367AAMLXDevice device;



        /// <summary>
        /// WPF dispatcher - slouší k tomu, aby události generované touto třídou, jejíž metody běži v jiných vláknech, mohly býti rovnou řazeny do hlavního GUI threadu aplikace
        /// </summary>
        private Dispatcher wpfDispatcher;

        
        public delegate void ErrorHandler(string message);
        /// <summary>
        /// Nastala nějaká chyba
        /// </summary>
        public event ErrorHandler OnError;

        public event PropertyChangedEventHandler PropertyChanged;


        //Konstruktor
        public MelexisPTC04(Dispatcher wpfDispatcher)
        {
            this.wpfDispatcher = wpfDispatcher;
        }


        ~MelexisPTC04()  // destructor
        {
            try
            {
                meritIdd = false;
                meritOut = false;
                if (device != null)
                    device.Destroy(true);
            }
            catch { }
        }


        //Implementace rozhraní (aby nadřazený prvek věděl, že se změnila nějaká vlastnost)
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }


        /// <summary>
        /// Vytvoření PSF090367AAMLXDevice
        /// </summary>
        public void Connect()
        {
            Thread thr = new Thread(() => createDevice());
            thr.Start();
        }


    /*  Nefunguje.  
        /// <summary>
        /// Vytvoření PSF090367AAMLXDevice - sse zadáním sériového portu
        /// </summary>
        private void createDevice(int cisloPortu)
        {
            try
            {
 
                CommManager CommMan = new CommManager("MPT.CommManager");
                PSF090367AAMLXDevice MyDev = new PSF090367AAMLXDevice("MPT.PSF090367AAMLXDevice");
                MPTChannel Chan = CommMan.Channels.CreateChannel(CVar(cisloPortu), ctSerial)); ???
                MyDev.Channel = Chan;
                Dev = MyDev;
            }
            catch (Exception ex)
            {
                throw new Exception("Chyba při vytváření objektů: " + ex.Message);
            }
        }*/


        /// <summary>
        /// Vytvoření PSF090367AAMLXDevice - scanování sériových portů
        /// </summary>
        private void createDevice()
        {
            PSF090367AAMLXManager PSFMan;
            ObjectCollection DevicesCol;
            
            try
            {
                //TODO: PSFMan = new PSF090367AAMLXManager("MPT.PSF090367AAMLXManager1");
                PSFMan = new PSF090367AAMLXManager();
                DevicesCol = PSFMan.ScanStandalone(DeviceType.dtSerial);

                if (DevicesCol.Count <= 0)
                    throw new Exception("No PTC-04 programmers found!");

                device = DevicesCol[0];

                if (DevicesCol.Count > 1)
                    for (int i = 1; i < DevicesCol.Count; i++)
                        DevicesCol[i].Destroy(true);

                connected = true; 
                NotifyPropertyChanged("Connected");
                NotifyPropertyChanged("Disconnected");
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    this.wpfDispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        OnError("Chyba při vytváření objektů: " + ex.Message + "\n\nJe zapnutá a připojená měřicí jednotka PTC-04?");
                    }));
            }
        }



        /// <summary>
        /// Nastartovíní kontinuálního měření (volat jen jednou při startu).
        /// </summary>
        private void startMereni()
        {
            Thread thr = new Thread(() => {
                while (ProbihaMereni)
                {
                    zmeritVse();
                }
            });
            thr.Start();
        }


        /// <summary>
        /// Změření všech hodnot
        /// </summary>
        private void zmeritVse()
        {   //Nelze číst z obou kanálů zároveň
            zmeritIdd(Kanal);
            zmeritOut(Kanal);
            Thread.Sleep(250); //Prodleva
        }


        /// <summary>
        /// Změření proudu
        /// </summary>
        private void zmeritIdd(int channel)
        {
            if (!MeritIdd)
            {
                valuesToNull(true, false);
                return;
            }

            try
            {  
                device.SelectedDevice = channel;
                float val = device.GetIdd();
                if (channel == 1) //Channel A (DieA)
                {
                    if (iddA.Hodnota != val)
                    {
                        iddA.Hodnota = val;
                    }
                }
                if (channel == 2) //Channel B (DieB)
                {
                    if (iddB.Hodnota != val)
                    {
                        iddB.Hodnota = val;
                    }
                }
            }
            catch { }
        }


        /// <summary>
        /// Změření Out
        /// </summary>
        private void zmeritOut(int channel)
        {
            if (!MeritOut)
            {
                valuesToNull(false, true);
                return;
            }
      
            try
            {  
                device.SelectedDevice = channel;
                Int64 val = device.GetAngleOut12bit();
                if (channel == 1) //Channel A (DieA)
                {
                    if (outA.Hodnota != val)
                    {
                        outA.Hodnota = val;
                    }
                }
                if (channel == 2) //Channel B (DieB)
                {
                    if (outB.Hodnota != val)
                    {
                        outB.Hodnota = val;
                    }
                }
            }
            catch { }
        }


        /// <summary>
        /// Nastavení všech měřených hodnot na null
        /// </summary>
        private void valuesToNull(bool iddToNull, bool outToNull)
        {
            if (iddToNull)
            {
                iddA.Hodnota = null;
                iddB.Hodnota = null;
            }
            if (outToNull)
            {
                outA.Hodnota = null;
                outB.Hodnota = null;
            }
        }

    }




    /// <summary>
    /// Naměřená hodnota z Melexis PTC-04
    /// </summary>
    public class MeasuredValue : INotifyPropertyChanged
    {

        private float? hodnota;
        /// <summary>
        /// Změřená hodnota
        /// </summary>
        public float? Hodnota
        {
            get { return this.hodnota; }
            set 
            {
                //Zaoktouhlení na 2 desetinná místa
                double? rounded = null;
                if (value != null)
                    rounded = Math.Round((double)value, 2, MidpointRounding.AwayFromZero);
                float? newVal = (float?)rounded;

                if (this.hodnota != newVal)
                {
                    this.hodnota = newVal;
                    NotifyPropertyChanged("Hodnota");
                    NotifyPropertyChanged("StrVal");
                }
                if (newVal != null)
                {
                    timeout.Stop();
                    timeout.Start();
                }
            }
        }


        /// <summary>
        /// Hodnota převedená na string
        /// </summary>
        public string StrVal
        {
            get { return this.hodnota.ToString(); }
        }


        /// <summary>
        /// Timer pro timeout měření (hodnota je validní pouze pár sekund po změření, pak ji tento timer shodí do null)
        /// </summary>
        private System.Timers.Timer timeout = new System.Timers.Timer();

        public event PropertyChangedEventHandler PropertyChanged;


        //Konstruktor
        public MeasuredValue()
        {
            timeout.Elapsed += new System.Timers.ElapsedEventHandler(timeout_Elapsed);
            timeout.Interval = 2000;
        }

        /// <summary>
        /// Vypršel timeout pro načtení hodnot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timeout.Stop();
            Hodnota = null;               
        }


        //Implementace rozhraní (aby nadřazený prvek věděl, že se změnila nějaká vlastnost)
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
