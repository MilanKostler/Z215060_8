using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HMI;

namespace LeakTesty
{
    /// <summary>
    /// Třída reprezentující měřák ATEQ. Vyvolává událost po obdržení naměřených dat a ukládá výsledky.
    /// </summary>
    public class Ateq: SeriovaKomunikace
    {
        /// <summary>
        /// Kolekce výsledků obdržených od ATEQu
        /// </summary>
        public List<string> Vysledky { get; set; }

        //události 
        public delegate void PrijataDataHandler(string namerenaData);
        public event PrijataDataHandler OnPrijataData;

        public Ateq(int baudRate, string portName)
            : base(baudRate, portName, "\r\n")
        {
            Vysledky = new List<string>();
        }

     
        /// <summary>
        /// Zpracování přijatých dat z ATEQu po RS232
        /// </summary>
        /// <param name="recData"></param>
        protected override void ZpracovaniDat(string recData)
        {
            Vysledky.Add(recData);
            if (OnPrijataData != null)
                OnPrijataData(recData);
        }


        /// <summary>
        /// Smaže všechny naměřené výsledky
        /// </summary>
        public void Clear()
        {
            Vysledky.Clear();
        }
    }
}
