using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Třída, která zajišťuje komunikaci po sériové lince s nějakým zařízením.
    /// Očekává příjem ACSI znaků s nějakým zakončovacím znakem (nebo znaky).
    /// Pokud není do 1 sekundy přijat zakončovací znak, přijatý buffer se
    /// vyresetuje, aby nedělal problémy při další komunikaci. Odvozená třída
    /// musí implementovat tělo apstraktní proceduře ZpracovaniDat, která má ve
    /// svém parametru StrBuffer přijatý string
    /// Pokud zakončovací znak = '', příjem se zakončuje automaticky časovačem.
    /// </summary>
    public abstract class SeriovaKomunikace
    {
        protected SerialPort seriovyPort = null;
        private string prijatyBuffer = "";  //Buffer přijatý po sériové lince
        private DispatcherTimer resetTimer; //Timer pro reset komunikace
        private DispatcherTimer endTimer;   //Timer pro automatické ukončení příjmu pokud je zakončovací znak ""
        private string zakonceni;           //Zakončení přijatých dat
        private bool autoTermination;       //Koonec přijímání se detekuje časovačem, nikoli zakončovacím znakem 


        /// <summary>
        /// Konstruktor - vytvoří objekt a rovnou se připojí. Při volání nutno ošetřit vyjímku.
        /// </summary>
        /// <param name="portName"></param>
        public SeriovaKomunikace(int baudRate, string portName, string zakonceni)
        {
            seriovyPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            seriovyPort.Handshake = Handshake.None;
            seriovyPort.DataReceived += new SerialDataReceivedEventHandler(seriovyPort_DataReceived);
            seriovyPort.Open(); //nutno ošetřit výjimku o úroveň výš
            this.zakonceni = zakonceni;
            autoTermination = (zakonceni == "");

            resetTimer = new DispatcherTimer();
            resetTimer.Interval = TimeSpan.FromMilliseconds(1000);
            resetTimer.Tick += new EventHandler(resetTimer_Tick);

            if (autoTermination)
            {
                endTimer = new DispatcherTimer();
                endTimer.Interval = TimeSpan.FromMilliseconds(100);
                endTimer.Tick += new EventHandler(endTimer_Tick);
            }
        }


        /// <summary>
        /// Obsluha přijetí bufferu ze sériového portu PLC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void seriovyPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[seriovyPort.BytesToRead];
            seriovyPort.Read(buffer, 0, buffer.Length);
            foreach (Byte prijatyByte in buffer)
                prijatyBuffer += (char)prijatyByte;

            resetTimer.Stop();
            resetTimer.Start();

            if (autoTermination)
            {
                endTimer.Stop();  //Resetování timeru pro automatický příjem
                endTimer.Start();
            }
            else
            {
                while (prijatyBuffer.IndexOf(zakonceni) > -1)   //cyklus vyzobe zakončené čárové kódy ze stringu. Např.: carovyKod "123<CR><LF>123<CR><LF>123<CR><LF>" zavolá 3x abstraktní metodu ZpracovaniDat s parametrem "123"
                {
                    int poziceKonce = prijatyBuffer.IndexOf(zakonceni);
                    string recData = prijatyBuffer.Substring(0, poziceKonce);
                    prijatyBuffer = prijatyBuffer.Substring(poziceKonce + zakonceni.Length);
                    ZpracovaniDat(recData);  //Vyvolání události
                }
            }
        }


        //Zpracování přijatých dat. Tuto metodu musí implementovat odvozená třída     
        protected abstract void ZpracovaniDat(string recData);
        

        /// <summary>
        /// Odeslání dat po sériovém portu
        /// </summary>
        /// <param name="buffer"></param>
        public void Poslat(string buffer)
        {
            seriovyPort.Write(buffer);
        }


        /// <summary>
        /// Timeout - čtečka již neposílá a nedočkali jsme se zakončení - smažeme přijatý buffer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void resetTimer_Tick(object sender, EventArgs e)
        {
            resetTimer.Stop();
            prijatyBuffer = "";
        }


        /// <summary>
        /// Přijata data - již nějakou dobu nic nepřišlo na sériový port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void endTimer_Tick(object sender, EventArgs e)
        {
            endTimer.Stop();
            ZpracovaniDat(prijatyBuffer);
            prijatyBuffer = "";
        }


        /// <summary>
        /// Vrátí seznam ASCI kódů zdrolového stringu v hexadecimálním tvaru
        /// </summary>
        /// <param name="zdtoj"></param>
        /// <returns></returns>
        public string HexaVypis(string zdtoj)
        {
            StringBuilder vypis = new StringBuilder();
            foreach (byte recByte in zdtoj)
                vypis.Append("$" + recByte.ToString("X2") + " ");
            return vypis.ToString();
        }
    }
}
