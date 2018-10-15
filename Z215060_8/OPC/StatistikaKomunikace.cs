using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace HMI.Debug
{
    /// <summary>
    /// Tříta, která spouží pro sběr statistických údajů o komunikaci
    /// </summary>
    public class StatistikaKomunikace
    {
        private Stopwatch stopwatch = new Stopwatch();

        private int casCyklu_ms;
        /// <summary>
        /// Čas posledního cyklu komunikace [ms]
        /// </summary>
        public int CasCyklu_ms { get { return casCyklu_ms; } }

        private int minCasCyklu_ms;
        /// <summary>
        /// Minimální čas cyklu komunikace [ms]
        /// </summary>
        public int MinCasCyklu_ms { get { return minCasCyklu_ms; } }

        private int maxCasCyklu_ms;
        /// <summary>
        /// Maximální čas cyklu komunikace [ms]
        /// </summary>
        public int MaxCasCyklu_ms { get { return maxCasCyklu_ms; } }

        public double CykluZaSekundu
        {
            get
            {
                double cyklu;
                if (CasCyklu_ms != 0)
                    cyklu = 1000 / (double)CasCyklu_ms;
                else
                    cyklu = 0;
                return Math.Round(cyklu, 1, MidpointRounding.AwayFromZero);
            }
        }


        /// <summary>
        /// Zda se má výpes z metody ToString zalamovat "\n", či nikoli.
        /// </summary>
        public bool ZalamovatVypis { get; set; }


        /// <summary>
        /// Textový výpis statistiky komunikace
        /// </summary>
        public string Vypis 
        { 
            get { return this.ToString(); } 
        }


        /// <summary>
        /// Jsou k dispozici nová statistická data o komunikaci s PLC
        /// </summary>
        public event Action OnDataReady;


        /// <summary>
        /// Změří čas cyklu (od posledního zavolání této metody)
        /// </summary>
        public void Zmerit()
        {
            if (stopwatch != null)
            {
                try
                {
                    casCyklu_ms = (int)stopwatch.ElapsedMilliseconds;  //(int)((double)casCyklu_ms * 0.9 + (double)stopwatch.ElapsedMilliseconds * 0.1)
                }
                catch
                {
                    casCyklu_ms = 0;
                }
                stopwatch.Stop();
                if (minCasCyklu_ms == 0 || casCyklu_ms < minCasCyklu_ms) //Minimální čas cyklu
                    minCasCyklu_ms = casCyklu_ms;
                if (casCyklu_ms > maxCasCyklu_ms)   //Maximální čas cyklu
                    maxCasCyklu_ms = casCyklu_ms;

                if (OnDataReady != null)
                    OnDataReady();
                stopwatch.Reset();
            }
            else
            {
                stopwatch = new Stopwatch();
            }
            stopwatch.Start();
        }


        /// <summary>
        /// Resetuje počítadla a zastaví stopování času
        /// </summary>
        public void Reset()
        {
            stopwatch.Stop();
            stopwatch.Reset();
            casCyklu_ms = 0;
            minCasCyklu_ms = 0;
            maxCasCyklu_ms = 0;
        }


        /// <summary>
        /// Výpis statistiky do stringu
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string oddelovac;
            if (ZalamovatVypis)
                oddelovac = "\n";
            else
                oddelovac = "   ";
            string vypis = String.Format("Čas cyklu komunikace:{0} {1} ms ({2} cklů/s){3}Min: {4} Max {5} ms", oddelovac, CasCyklu_ms, CykluZaSekundu, oddelovac, MinCasCyklu_ms, MaxCasCyklu_ms);
            return vypis;
        }
    }
}
