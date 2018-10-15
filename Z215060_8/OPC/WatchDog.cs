using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Třída ke sledování, zda něco nevytuhlo. WatchDog je stále nutné resetovat, nebo mu blikat s vlastností "LiveBit", jinak vyhodí událost "OnTimeout"
    /// </summary>
    class WatchDog
    {
        private DispatcherTimer timeout;
        private bool liveBit;
        public bool LiveBit
        {
            get { return liveBit; }
            set 
            {
                if (liveBit != value)
                {
                    timeout.Stop();
                    timeout.Start();
                    aktivni = true;
                    liveBit = value;
                    if (OnReseted != null)
                        OnReseted();
                }
            }
        }

        private bool aktivni;
        public bool Aktivni { get { return aktivni; } }

        //deklarace událostí
        public delegate void TimeoutHandler();
        public event TimeoutHandler OnTimeout;
        public delegate void ResetedHandler();
        public event ResetedHandler OnReseted;


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="timeoutSec">Timeout [s] (do této doby je nutné WatchDog resetovat)</param>
        public WatchDog(int timeoutSec)
        {
            timeout = new DispatcherTimer();
            timeout.Interval = TimeSpan.FromSeconds(timeoutSec);
            timeout.Tick += new EventHandler(timeout_Tick);
            timeout.Start();
            aktivni = true;
        }


        /// <summary>
        /// Ošetření události, že vypršel timeout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timeout_Tick(object sender, EventArgs e)
        {
            timeout.Stop();
            aktivni = false;
            if (OnTimeout != null)
                OnTimeout();
        }


        /// <summary>
        /// Resetuje timer pro hlídání timeoutu 
        /// </summary>
        public void Reset()
        {
            timeout.Stop();
            timeout.Start();
            aktivni = true;
            if (OnReseted != null)
                OnReseted();
        }

    }
}
