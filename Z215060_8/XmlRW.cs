using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows;
using System.Xml.Linq;

namespace Sablona
{
    /// <summary>
    /// Tato třída slouží k zápsu a čtení jednotlivých hodnot do a z XML filu podobně,
    ///     jako to šlo v Delphi s INI filem.
    /// </summary>
    class XmlRW
    {
        private string strFile;
        public string StrRootElement = "Data";

        //Konstruktor
        public XmlRW(string strFile)
        {
            this.strFile = strFile;
        }


        /// <summary>
        /// Načtení požadované hodnoty jako string. Pokud není nalezena, funkce vrátí null
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        private string Read(string element, string atribut)
        {
            XmlTextReader xmlSoubor = new XmlTextReader(strFile);
            xmlSoubor.WhitespaceHandling = WhitespaceHandling.None;
            try
            {
                string vysledek = null;
                bool nalezeno = false;
                while (xmlSoubor.Read() && !nalezeno)
                {
                    if (xmlSoubor.NodeType == XmlNodeType.Element)
                        if (xmlSoubor.Depth == 1 && xmlSoubor.Name == element)  //v hloubce 1 byl nalezen hledaný element
                            if (xmlSoubor.GetAttribute(atribut) != null)    //Nalezen hledaný atribut
                            {
                                nalezeno = true;
                                vysledek = xmlSoubor.GetAttribute(atribut);
                            }
                }
                return vysledek;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (xmlSoubor != null)
                    xmlSoubor.Close();
            }
        }


        /// <summary>
        /// Načtení požadované hodnoty z XMLsouboru. Pokud se načtení nezdaří, vrátí se "defaultValue"
        /// </summary>
        /// <param name="element">XML Element hloubky 1 (Case sensitive)</param>
        /// <param name="atribut">Atribut XML elementu (Case sensitive)</param>
        /// <param name="defaultValue">Hodnota, kterou vunkce vrátí, pokud se nepodaří nalézt zadaná data</param>
        /// <returns></returns>
        public bool ReadBool(string element, string atribut, bool defaultValue)
        {
            string strVysledek = this.Read(element, atribut);
            if (strVysledek != null)
            {
                if (strVysledek == "1")
                    return true;
                else
                    return false;
            }
            else
                return defaultValue;
        }


        /// <summary>
        /// Načtení požadované hodnoty z XMLsouboru. Pokud se načtení nezdaří, vrátí se "defaultValue"
        /// </summary>
        /// <param name="element">XML Element hloubky 1</param>
        /// <param name="atribut">Atribut XML elementu</param>
        /// <param name="defaultValue">Hodnota, kterou vunkce vrátí, pokud se nepodaří nalézt zadaná data</param>
        /// <returns></returns>
        public string ReadString(string element, string atribut, string defaultValue)
        {
            string strVysledek = this.Read(element, atribut);
            if (strVysledek != null)
            {
                return strVysledek;
            }
            else
                return defaultValue;
        }


        /// <summary>
        /// Načtení požadované hodnoty z XMLsouboru. Pokud se načtení nezdaří, vrátí se "defaultValue"
        /// </summary>
        /// <param name="element">XML Element hloubky 1</param>
        /// <param name="atribut">Atribut XML elementu</param>
        /// <param name="defaultValue">Hodnota, kterou vunkce vrátí, pokud se nepodaří nalézt zadaná data</param>
        /// <returns></returns>
        public int ReadInt(string element, string atribut, int defaultValue)
        {
            string strVysledek = this.Read(element, atribut);      
            try
            {
                int vysledek;
                vysledek = int.Parse(strVysledek);
                return vysledek;
            }
            catch
            {
                return defaultValue;
            }
        }



        /// <summary>
        /// Načtení požadované hodnoty z XMLsouboru. Pokud se načtení nezdaří, vrátí se "defaultValue"
        /// </summary>
        /// <param name="element">XML Element hloubky 1</param>
        /// <param name="atribut">Atribut XML elementu</param>
        /// <param name="defaultValue">Hodnota, kterou vunkce vrátí, pokud se nepodaří nalézt zadaná data</param>
        /// <returns></returns>
        public double ReadFloat(string element, string atribut, double defaultValue)
        {
            string strVysledek = this.Read(element, atribut);
            try
            {
                double vysledek;
                vysledek = double.Parse(strVysledek);
                return vysledek;
            }
            catch
            {
                return defaultValue;
            }
        }



        /// <summary>
        /// Zapsání požadované hodnoty jako string. 
        /// Pokud není nalezen element, nebo atribut, vytvoří se nový
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        private void Write(string element, string atribut, string value)
        {
            XmlDocument xmlSouborDoc = new XmlDocument();
            try
            {
                xmlSouborDoc.Load(strFile);
                XmlNodeList nodeList = xmlSouborDoc.GetElementsByTagName(element);

                if (nodeList.Count == 0)
                {
                    // vytvoření nového elementu
                    XmlElement newElement = xmlSouborDoc.CreateElement(element);
                    newElement.SetAttribute(atribut, value);
                    xmlSouborDoc.DocumentElement.AppendChild(newElement);
                    xmlSouborDoc.Save(strFile);
                }
                else
                {
                    // Editace existujícího elementu
                    XDocument xmlXDoc = XDocument.Load(strFile);
                    var query = from c in xmlXDoc.Elements(StrRootElement).Elements(element) select c;
                    foreach (XElement book in query)
                    {
                        book.SetAttributeValue(atribut, value);
                    }
                    xmlXDoc.Save(strFile);
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// Zapsání požadované hodnoty do XML souboru do hloubky 1. 
        /// Pokud není nalezen element, nebo atribut, vytvoří se nový
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        public void WriteString(string element, string atribut, string value)
        {
            this.Write(element, atribut, value);
        }


        /// <summary>
        /// Zapsání požadované hodnoty do XML souboru do hloubky 1. 
        /// Pokud není nalezen element, nebo atribut, vytvoří se nový
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        public void WriteBool(string element, string atribut, bool value)
        {
            string strVal;
            if (value)
                strVal = "1";
            else
                strVal = "0";
            this.Write(element, atribut, strVal);
        }

        /// <summary>
        /// Zapsání požadované hodnoty do XML souboru do hloubky 1. 
        /// Pokud není nalezen element, nebo atribut, vytvoří se nový
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        public void WriteInt(string element, string atribut, int value)
        {
            string strVal = value.ToString();
            this.Write(element, atribut, strVal);
        }



        /// <summary>
        /// Zapsání požadované hodnoty do XML souboru do hloubky 1. 
        /// Pokud není nalezen element, nebo atribut, vytvoří se nový
        /// </summary>
        /// <param name="element"></param>
        /// <param name="atribut"></param>
        /// <returns></returns>
        public void WriteFloat(string element, string atribut, double value)
        {
            string strVal = value.ToString();
            this.Write(element, atribut, strVal);
        }

    }
}
