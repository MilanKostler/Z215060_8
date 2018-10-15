using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HMI
{
    /// <summary>
    /// ViewModel stavů stroje
    /// </summary>
    public class StavStrojeViewModel: ViewModelBase
    {
        /// <summary>
        /// Reference na stav stroje
        /// </summary>
        private StavStroje stavStroje;


        /// <summary>
        /// Text stavu (Např. TOTAL STOP)
        /// </summary>
        public string Text
        {
            get
            {
                switch (stavStroje.CisloStavu)
                {
                    //case 0:
                    //    return "";
                    case (int)StavyStroje.TotalStop:
                        return "TOTAL STOP";
                    case (int)StavyStroje.Iniciace:
                        return "INICIACE";
                    case (int)StavyStroje.Pripraven:
                        return "PŘIPRAVEN";
                    case (int)StavyStroje.Alarm:
                        return "ALARM";
                    case (int)StavyStroje.Serizovani:
                        return "SEŘIZOVÁNÍ";
                    case (int)StavyStroje.VChodu:
                        return "V CHODU";
                    case (int)StavyStroje.Zastaveno:
                        return "ZASTAVENO";
                    case (int)StavyStroje.Zastavovani:
                        return "ZASTAVOVÁNÍ";
                    default:
                        return "Stav č. " + stavStroje.CisloStavu.ToString();
                }
            }
        }


        /// <summary>
        /// Barva, jakou se má stav zobrazit na vizualizaci
        /// </summary>
        public System.Windows.Media.Brush Barva
        {
            get
            {
                if ((stavStroje.CisloStavu == (int)StavyStroje.TotalStop) || (stavStroje.CisloStavu == (int)StavyStroje.Alarm))
                    return System.Windows.Media.Brushes.Red;
                else
                    return System.Windows.Media.Brushes.Black;
            }
        }


        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="stavStroje">Reference na stav stroje</param>
        public StavStrojeViewModel(StavStroje stavStroje)
        {
            this.stavStroje = stavStroje;
            this.stavStroje.OnStateChanged += () =>
                {

                    NotifyPropertyChanged("Barva");
                    NotifyPropertyChanged("Text");
                };
        }


        /// <summary>
        /// Vrátí stav stroje v textové podobě
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
    }
}
