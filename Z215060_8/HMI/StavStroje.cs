using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HMI
{
    /// <summary>
    /// Stavy stroje. Jediné místo v programu, kde se přiřazují čísla stavů stavům.
    /// </summary>
    public enum StavyStroje { TotalStop = 1, Iniciace = 2, Pripraven = 3, Alarm = 4, Serizovani = 5, VChodu = 6, Zastaveno = 7, Zastavovani = 8 }; 


    /// <summary>
    /// Třída reprezentující stav stroje. Přetěžuje operátory "==" a "!=" a při porovnívání umí spolupracovat s enum StavyStroje.
    /// Příklady porovnání:
    /// (Stav == StavyStroje.TotalStop); (Stav.Equals(StavyStroje.TotalStop)); (Stav.Equals(new StavStroje(1));
    /// Příklad přiřazení stavu:
    /// Stav.SetState(opcClient.PrijataData.StavStroje.Value);
    /// </summary>
    public class StavStroje
    {
        private int cisloStavu;
        /// <summary>
        /// Číslo stavu stroje
        /// </summary>
        public int CisloStavu { get {return cisloStavu; } }


        /// <summary>
        /// Nastala změna hodnoty stavu stroje
        /// </summary>
        public event Action OnStateChanged;


        public StavStroje()
        {
        }

        /// <summary>
        /// Konstruktor se zadání čísla stavu
        /// </summary>
        /// <param name="stav">Číslo stavu stroje</param>
        public StavStroje(int stav)
        {
            this.cisloStavu = stav;
        }


        /// <summary>
        /// Vrátí true, pokud se shoduje číslo stavu
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="rs"></param>
        /// <returns></returns>
        public static bool operator ==(StavStroje ls, StavyStroje rs)
        {
            return (ls.cisloStavu == (int)rs);
        }


        /// <summary>
        /// Vrátí true, pokud se neshoduje číslo stavu
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="rs"></param>
        /// <returns></returns>
        public static bool operator !=(StavStroje ls, StavyStroje rs)
        {
            return (ls.cisloStavu != (int)rs);
        }


        /// <summary>
        /// Vrátí true, pokud je objekt předaný parametrem typu "StavStroje", není null a shoduje se číslo stavu
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            StavStroje ss = obj as StavStroje;
            if (ss == null)
                return false;

            return (cisloStavu == ss.cisloStavu);
        }


        /// <summary>
        /// Vrátí true, pokud se shoduje číslo stavu
        /// </summary>
        /// <param name="stav"></param>
        /// <returns></returns>
        public bool Equals(StavStroje stav)
        {
            if (stav == null)
                return false;

            return (cisloStavu == stav.cisloStavu);
        }


        /// <summary>
        /// Vrátí true, pokud se shoduje číslo stavu se zadaným parametrem
        /// </summary>
        /// <param name="stav"></param>
        /// <returns></returns>
        public bool Equals(StavyStroje stav)
        {
            return (cisloStavu == (int)stav);
        }


        public override int GetHashCode()
        {
            return cisloStavu;
        }


        /// <summary>
        /// Nastaví číslo stavu dle zadaného parametru
        /// </summary>
        /// <param name="cislo">Číslo stavu z PLC</param>
        public void SetState(int cislo)
        {
            if (cisloStavu != cislo)
            {
                cisloStavu = cislo;
                if (OnStateChanged != null)
                    OnStateChanged();
            }
        }


        /// <summary>
        /// Nastaví stav dle zadaného parametru
        /// </summary>
        /// <param name="stav"></param>
        public void SetState(StavyStroje stav)
        {
            SetState((int)stav);
        }
    }
}
