using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Sablona;
using System.ComponentModel;
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Role jako v databázi - každá role má přidělená určitá oprávnění (např. admin)
    /// </summary>
    public class Role : INotifyPropertyChanged
    {
        private string hashHesla = null;
        private string xmlFile;  //XML soubor pro ukládání a načítání heškódu hesla
        private string nazevRole;  //Název role - např admin. Slouží k uložení hesla do XML souboru
        private DispatcherTimer autologout;  //Timer na automatické odhlášení po určitém čase
        private bool loggedOn;
        /// <summary>
        /// Zda je užidatel přihlážen heslem k tého roli
        /// </summary>
        public bool LoggedOn
        {
            get { return loggedOn; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void AutologoutHandler();
        public event AutologoutHandler OnAutologout;

        /// <summary>
        /// Konstruktor se zadáním XML souboru pro ukládání a načítání heškódu hesla
        /// </summary>
        /// <param name="xmlFile">XML soubor pro ukládání a načítání heškódu hesla</param>
        /// <param name="nazevRole">Název role - např admin. Bude sloužit k uložení hesla do XML souboru</param>
        /// <param name="autoLogoutSec">Za kolik minut se má automaticky odlogovat. 0 - funkce autologout je vypnuta</param>
        public Role(string nazevRole, string xmlFile, int autoLogoutMin)
        {
            this.xmlFile = xmlFile;
            this.nazevRole = nazevRole;
            if (autoLogoutMin > 0)  //Aktivace funkce autologout
            {
                autologout = new DispatcherTimer();
                autologout.Interval = TimeSpan.FromMinutes(autoLogoutMin);
                autologout.Tick += (se, ea) =>
                {
                    //autologout.Stop();  Pokud je otevřen modální dialog, autologon se nepovede. Proto se musí zkoušet znovu a znovu.
                    if (OnAutologout != null)
                        OnAutologout();
                };
            }
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
        /// Přihlášení k roli
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool LogOn(string password)
        {
            loggedOn = OvereniHesla(password);   
            NotifyPropertyChanged("LoggedOn");
            if (autologout != null)
            {
                autologout.Stop();
                autologout.Start();
            }
            return (loggedOn);
        }


        /// <summary>
        /// Odhlášení z role
        /// </summary>
        public void Logout()
        {
            if (loggedOn)
            {
                loggedOn = false;
                NotifyPropertyChanged("LoggedOn");
                if (autologout != null)
                    autologout.Stop();
            }
        }


        /// <summary>
        /// Ověření, zda se heslo shoduje s požadovaným
        /// </summary>
        /// <param name="heslo"></param>
        /// <returns></returns>
        public bool OvereniHesla(string psw)
        {
            hashHesla = loadHashFromFile(xmlFile);
            if (hashHesla != null)
                if (hashHesla == computeHash(psw))
                    return true;

            return false;
        }


        /// <summary>
        /// Vrátí SHA256 hashcode hesla v hexa formátu jako string
        /// </summary>
        /// <param name="psw"></param>
        /// <returns></returns>
        private string computeHash(string psw)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] pswBytes = encoding.GetBytes(psw);  //Převod zadaného hesla na pole bytů
            SHA256Managed sha2 = new SHA256Managed();
            byte[] hash = sha2.ComputeHash(pswBytes);  //Výpočet SHA-2

            StringBuilder hashPsw = new StringBuilder(hash.Length * 2);  //Převod hash kódu zadaného hesla na "hexadecimální" string
            foreach (byte by in hash)
                hashPsw.AppendFormat("{0:x2}", by);

            return hashPsw.ToString();
        }


        /// <summary>
        /// Načte hashcode hesla z xml souboru. V případě neúspěchu vrátí null
        /// </summary>
        /// <param name="xmlFile"></param>
        private string loadHashFromFile(string xmlFile)
        {
            XmlRW xml = new XmlRW(xmlFile);
            string loaded = xml.ReadString(nazevRole, "PswHash", "");

            if (loaded != "")
                return loaded;
            else
                return null;
        }


        /// <summary>
        /// Uloží hashcode zadaného hesla do XML souboru
        /// </summary>
        public void SaveHashHelsa(string password)
        {
            XmlRW xml = new XmlRW(xmlFile);
            xml.WriteString(nazevRole, "PswHash", computeHash(password));
        }

    }
}
