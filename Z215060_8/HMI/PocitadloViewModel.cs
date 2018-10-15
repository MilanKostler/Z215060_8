using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HMI
{
    /// <summary>
    /// ViewModel počítadla OK, NOK a Celkem
    /// </summary>
    public class PocitadloViewModel: ViewModelBase
    {
        /// <summary>
        /// Model počítadla
        /// </summary>
        private Pocitadlo pocitadlo;

        /// <summary>
        /// Celkový počet kusů
        /// </summary>
        public UInt32 Celkem { get { return Ok + Nok; } }

        private UInt32 ok;
        /// <summary>
        /// Počet NOK kusů
        /// </summary>
        public UInt32 Ok { get { return ok; } }

        private UInt32 nok;
        /// <summary>
        /// Počet NOK kusů
        /// </summary>
        public UInt32 Nok { get { return nok; } }



        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="pocitadlo">Reference na model počítadla</param>
        public PocitadloViewModel(Pocitadlo pocitadlo)
        {
            this.pocitadlo = pocitadlo;
            ok = this.pocitadlo.Ok;
            nok = this.pocitadlo.Nok;
            NotifyPropertyChanged("Ok");
            NotifyPropertyChanged("Nok");
            NotifyPropertyChanged("Celkem");
            this.pocitadlo.OnValueChanged += () => { refreshCounter(); };
        }



        /// <summary>
        /// Aktualizace stavu počítadel dle modelu
        /// </summary>
        private void refreshCounter()
        {
            if (pocitadlo.Ok != ok)
            {
                ok = pocitadlo.Ok;
                NotifyPropertyChanged("Ok");
                NotifyPropertyChanged("Celkem");
            }
            if (pocitadlo.Nok != nok)
            {
                nok = pocitadlo.Nok;
                NotifyPropertyChanged("Nok");
                NotifyPropertyChanged("Celkem");
            }
        }
    }
}
