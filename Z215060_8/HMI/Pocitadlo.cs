using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HMI
{
    /// <summary>
    /// Počítadlo OK, NOK a Celkem
    /// </summary>
    public class Pocitadlo
    {
        /// <summary>
        /// Nastala změna hodnoty počítadla
        /// </summary>
        public event Action OnValueChanged;


        private UInt32 ok = 0;
        /// <summary>
        /// Počet OK kusů
        /// </summary>
        public UInt32 Ok 
        { 
            get { return ok; }
            set 
            {
                if (ok != value)
                {
                    ok = value;
                    if (OnValueChanged != null)
                        OnValueChanged();
                }
            }
        }


        private UInt32 nok = 0;
        /// <summary>
        /// Počet NOK kusů
        /// </summary>
        public UInt32 Nok
        {
            get { return nok; }
            set
            {
                if (nok != value)
                {
                    nok = value;
                    if (OnValueChanged != null)
                        OnValueChanged();
                }
            }
        }
    }
}
