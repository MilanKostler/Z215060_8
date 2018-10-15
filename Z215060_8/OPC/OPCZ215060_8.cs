//******************************************************************************
//
//    Tato třída je odvozená od třídy OPCClient, kterou rozšiřuje o data a metody 
//      napsané na míru konkrétnímu zařízení. Zde se definují items (proměnné
//      pro komunikaci). Každá proměnná je buď jen pro zápis na OPC server, nebo
//      jen pro čtení.   
//
//    Obsahuje strukturuy s odeslánými a přijatými proměnnými, které jsou určeny 
//      pro používání z nadřazených tříd (programu pro konkrétní stroj).       
//
//    V konstruktoru je nutné vytvořit instance všech proměnných, které byly 
//      nadefinovány ve strukturách pro přijetí a pro zápis. Každé proměnné se 
//      musí říci, jaký má ItemName, do jaké hashtabulky se má přidat 
//      (hashTblItemsCteni nebo hashTblItemsZapis) a zda má mát hodnotu "Value" 
//      jen pro čtení.
//
//******************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using HMI;

namespace Z215060_8
{
    //Nadefinování struktury pro přijatá data z OPC serveru
    public struct TPrijataData //Společná data pro celý stroj
    {
        public ItemBit32R LiveBitCopy;  //Kopie live bitu, který PLC přijalo         
        public ItemBit32R EnableManual;  //Povolení aktivovat na PC ruční ovládání. Pokud bit zhasne, manuální ovládání se automaticky zruší
        public ItemBit32R KlicekMan;  //Klíček MAN/AUTO v poloze MAN
        public ItemBit32R EnableNastaveni;  //Lze otevřít dialogy nastavení receptury apod.
        public ItemBit32R MachineIsBlocked;  //Zařízení je zablokováno nadřazeným systémem
        public ItemBit32R TotalStop;  //Stroj je ve stavu Total Stop - není provedena iniciace
        public ItemBit32R Ptc4Kanal;  //Melexis PTC-4 - na kterém kanálu měřit: false..A (levá), true..B (pravá)
        public ItemBit32R Ptc4MerIdd;  //Požadavek na měření IDD [mA] přístrojem Melexis PTC-4 na požadované straně
        public ItemBit32R Ptc4MerOut;  //Požadavek na měření Out [lsb] přístrojem Melexis PTC-4 na požadované straně
        public ItemUInt32R DwErr1;  //DaubleWord alarmů
        public ItemUInt32R DwErr2;  //DaubleWord alarmů
        public ItemUInt16R StavStroje;  //Stav stroje

        public TPrijatoStation L; //Data levé strany
        public TPrijatoStation P; //Data pravé strany
    }

    public struct TPrijatoStation //přijatá data stanice (v tomto přípdaě strany)
    {
        public ItemUInt32R DwManual1;  //Bity pro manuální ovládání - vstupy
        public ItemUInt32R DwManual2;  //Bity pro manuální ovládání - vstupy
        public ItemUInt32R DwErr1;  //DaubleWord alarmů
        public ItemUInt32R DwErr2;  //DaubleWord alarmů
        public ItemUInt32R DwErr3;  //DaubleWord alarmů
        public ItemUInt16R Message;  //Message pro operátora
        public ItemUInt32R CountOk;  //Počítadlo OK výrobků
        public ItemUInt32R CountNok;  //Počítadlo NOK výrobků
    }

    //Nadefinování struktury pro data k zápisu na OPC server
    public struct TDataKOdeslani
    {
        //Společná data pro celý stroj
        public ItemBit32W LiveBit;  //Negace live bitu, který PC přijalo
        public ItemBit32W ChybaPc;  //Nastala chyba PC (nešlo uložit soubor apod.)
        public ItemBit32W RucniOvladani;  //Zobrazen dialog pro mauální ovládání jednotlivých prvků stroje
        public ItemBit32W BezTraceability;  //Aktivován režim bez systému Traceability (zatím bez funkce)
        public ItemBit32W OtevrenDialog;  //Na PC otevřen důležitý dialog, nelze spustit test
        public ItemBit32W Ack;  //HMI alarm acknowledge

        public TOdeslanoStation L; //Data levé strany
        public TOdeslanoStation P; //Data pravé strany
    }

    public struct TOdeslanoStation //data pro odeslání do stanice (v tomto přípdaě strany)
    {
        public ItemBit32W AteqResAvailable;  //Přijat string o měření z Ategu. Bit se nahodí na 2 s po obdržení  výsledku přes RS232
        public ItemBit32W Ptc4_IDDmA_Valid;  //Hodnota IDD z Melexisu je aktuální a platná
        public ItemBit32W Ptc4_Outlsb_Valid;  //Hodnota Out z Melexisu je aktuální a platná
        public ItemBit32W ResetStatistik;  //Reset statistik OK, NOK
        public ItemBit32W HomePos;  //Požadavek na inicializaci 
        public ItemUInt32W DwManual1;  //Bity pro manuální ovládání - výstupy
        public ItemUInt32W DwManual2;  //Bity pro manuální ovládání - výstupy
        public ItemDoubleW IDD_mA;  //IDD [mA] (Hodnota z Melexis PTC-04)
        public ItemInt16W Out_lsb;  //Out [lsb] (Hodnota z Melexis PTC-04)
        public ItemStringW AteqResult;  //Výsledek testu z Atequ ve tvaru "<02>:  2.50 bar:(OK):  017 Pa" max délka 40 znaků
    }

    public class OPCZ215060_8 : OPCClient
    {
        public TDataKOdeslani DataKOdeslani;    //Data pro poslání do PLC, k dispozici dalším Unitám a objektům programu     
        private TPrijataData prijataData;       //Data přijatá z PLC, k dispozici dalším Unitám a objektům programu
        public TPrijataData PrijataData { get { return prijataData; } }

        /// <summary>
        /// Vytvoření instance OPC clienta se zadáním parametru rychlosti opakovaného načítání
        /// Pořadí, v jakém jsou zde vytářeny OpcItems pro zápis se budou sekvenčně zapisovat z OPC serveru do PLC.
        /// OpcItems pro čtení se načítají z cashe OPC serveru.
        /// </summary>
        /// <param name="prodlevaCteni">Čas [ms], za jak dlouho se po dokončení jednoho přečtení a zápisu opět zahájí čtení z OPC serveru. (Ovlivňuje rychlost komunikace a vytížení procesoru)</param>
        public OPCZ215060_8(int prodlevaCteni) :
            base(prodlevaCteni)
        {
            //Items pro čtení
            string dwBits1r = "Z215060_8.S7-300.Plc2Pc.DwBits1"; //Název (ID) 32bitového konteineru na OPC serveru 
            prijataData.LiveBitCopy = new ItemBit32R(dwBits1r, itemsCteni, ".0 - LiveBitCopy", 0);
            prijataData.LiveBitCopy.SetContainersSwapBytes(false);
            prijataData.EnableManual = new ItemBit32R(dwBits1r, itemsCteni, ".1 - EnableManual", 1);
            prijataData.KlicekMan = new ItemBit32R(dwBits1r, itemsCteni, ".2 - KlicekMan", 2);
            prijataData.EnableNastaveni = new ItemBit32R(dwBits1r, itemsCteni, ".3 - EnableNastaveni", 3);
            prijataData.MachineIsBlocked = new ItemBit32R(dwBits1r, itemsCteni, ".4 - MachineIsBlocked", 4);
            prijataData.TotalStop = new ItemBit32R(dwBits1r, itemsCteni, ".5 - TotalStop", 5);
            prijataData.Ptc4Kanal = new ItemBit32R(dwBits1r, itemsCteni, ".6 - Ptc4Kanal", 6);
            prijataData.Ptc4MerIdd = new ItemBit32R(dwBits1r, itemsCteni, ".7 - Ptc4MerIdd", 7);
            prijataData.Ptc4MerOut = new ItemBit32R(dwBits1r, itemsCteni, ".8 - Ptc4MerOut", 8);

            prijataData.DwErr1 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.DwErr1", itemsCteni) { SwapBytes = true };
            prijataData.DwErr2 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.DwErr2", itemsCteni) { SwapBytes = true };
            prijataData.StavStroje = new ItemUInt16R("Z215060_8.S7-300.Plc2Pc.StavStroje", itemsCteni);
           
            //Levá strana
            prijataData.L.DwManual1 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.DwManual1", itemsCteni) { SwapBytes = true };
            prijataData.L.DwManual2 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.DwManual2", itemsCteni) { SwapBytes = true };
            prijataData.L.DwErr1 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.DwErr1", itemsCteni) { SwapBytes = true };
            prijataData.L.DwErr2 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.DwErr2", itemsCteni) { SwapBytes = true };
            prijataData.L.DwErr3 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.DwErr3", itemsCteni) { SwapBytes = true };
            prijataData.L.Message = new ItemUInt16R("Z215060_8.S7-300.Plc2Pc.L.Message", itemsCteni);
            prijataData.L.CountOk = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.CountOk", itemsCteni);
            prijataData.L.CountNok = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.L.CountNok", itemsCteni);

            //Pravá strana
            prijataData.P.DwManual1 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.DwManual1", itemsCteni) { SwapBytes = true };
            prijataData.P.DwManual2 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.DwManual2", itemsCteni) { SwapBytes = true };
            prijataData.P.DwErr1 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.DwErr1", itemsCteni) { SwapBytes = true };
            prijataData.P.DwErr2 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.DwErr2", itemsCteni) { SwapBytes = true };
            prijataData.P.DwErr3 = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.DwErr3", itemsCteni) { SwapBytes = true };
            prijataData.P.Message = new ItemUInt16R("Z215060_8.S7-300.Plc2Pc.P.Message", itemsCteni);
            prijataData.P.CountOk = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.CountOk", itemsCteni);
            prijataData.P.CountNok = new ItemUInt32R("Z215060_8.S7-300.Plc2Pc.P.CountNok", itemsCteni);



            //Items pro zápis
            string dwBits1w = "Z215060_8.S7-300.Plc2Pc.DwBits1"; //Název (ID) 32bitového konteineru na OPC serveru 
            DataKOdeslani.LiveBit = new ItemBit32W(dwBits1w, itemsZapis, ".0 - LiveBit", 0);
            DataKOdeslani.ChybaPc = new ItemBit32W(dwBits1w, itemsZapis, ".1 - ChybaPc", 1);
            DataKOdeslani.RucniOvladani = new ItemBit32W(dwBits1w, itemsZapis, ".2 - RucniOvladani", 2);
            DataKOdeslani.BezTraceability = new ItemBit32W(dwBits1w, itemsZapis, ".3 - BezTraceability", 3);
            DataKOdeslani.OtevrenDialog = new ItemBit32W(dwBits1w, itemsZapis, ".4 - OtevrenDialog", 4);
            DataKOdeslani.Ack = new ItemBit32W(dwBits1w, itemsZapis, ".5 - Ack", 5);

            //Levá strava
            string dwBits1wl = "Z215060_8.S7-300.Pc2Plc.L.DwBits1"; //Název (ID) 32bitového konteineru na OPC serveru 
            DataKOdeslani.L.AteqResAvailable = new ItemBit32W(dwBits1wl, itemsZapis, ".0 - AteqResAvailable", 0);
            DataKOdeslani.L.Ptc4_IDDmA_Valid = new ItemBit32W(dwBits1wl, itemsZapis, ".1 - Ptc4_IDDmA_Valid", 1);
            DataKOdeslani.L.Ptc4_Outlsb_Valid = new ItemBit32W(dwBits1wl, itemsZapis, ".2 - Ptc4_Outlsb_Valid", 2);
            DataKOdeslani.L.ResetStatistik = new ItemBit32W(dwBits1wl, itemsZapis, ".3 - ResetStatistik", 3);
            DataKOdeslani.L.HomePos = new ItemBit32W(dwBits1wl, itemsZapis, ".7 - HomePos", 7);

            DataKOdeslani.L.DwManual1 = new ItemUInt32W("Z215060_8.S7-300.Pc2Plc.L.DwManual1", itemsZapis) { SwapBytes = true };
            DataKOdeslani.L.DwManual2 = new ItemUInt32W("Z215060_8.S7-300.Pc2Plc.L.DwManual2", itemsZapis) { SwapBytes = true };
            DataKOdeslani.L.IDD_mA = new ItemDoubleW("Z215060_8.S7-300.Pc2Plc.L.IDD_mA", itemsZapis);
            DataKOdeslani.L.Out_lsb = new ItemInt16W("Z215060_8.S7-300.Pc2Plc.L.Out_lsb", itemsZapis);
            DataKOdeslani.L.AteqResult = new ItemStringW("Z215060_8.S7-300.Pc2Plc.L.AteqResult", itemsZapis);

            //Pravá strava
            string dwBits1wp = "Z215060_8.S7-300.Pc2Plc.P.DwBits1"; //Název (ID) 32bitového konteineru na OPC serveru
            DataKOdeslani.P.AteqResAvailable = new ItemBit32W(dwBits1wp, itemsZapis, ".0 - AteqResAvailable", 0);
            DataKOdeslani.P.Ptc4_IDDmA_Valid = new ItemBit32W(dwBits1wp, itemsZapis, ".1 - Ptc4_IDDmA_Valid", 1);
            DataKOdeslani.P.Ptc4_Outlsb_Valid = new ItemBit32W(dwBits1wp, itemsZapis, ".2 - Ptc4_Outlsb_Valid", 2);
            DataKOdeslani.P.ResetStatistik = new ItemBit32W(dwBits1wp, itemsZapis, ".3 - ResetStatistik", 3);
            DataKOdeslani.P.HomePos = new ItemBit32W(dwBits1wp, itemsZapis, ".7 - HomePos", 7);

            DataKOdeslani.P.DwManual1 = new ItemUInt32W("Z215060_8.S7-300.Pc2Plc.P.DwManual1", itemsZapis) { SwapBytes = true };
            DataKOdeslani.P.DwManual2 = new ItemUInt32W("Z215060_8.S7-300.Pc2Plc.P.DwManual2", itemsZapis) { SwapBytes = true };
            DataKOdeslani.P.IDD_mA = new ItemDoubleW("Z215060_8.S7-300.Pc2Plc.P.IDD_mA", itemsZapis);
            DataKOdeslani.P.Out_lsb = new ItemInt16W("Z215060_8.S7-300.Pc2Plc.P.Out_lsb", itemsZapis);
            DataKOdeslani.P.AteqResult = new ItemStringW("Z215060_8.S7-300.Pc2Plc.P.AteqResult", itemsZapis);



          /*DataKOdeslani.Prog1 = new ItemUInt16("Z215060_8.S7-300.Pc2Plc.Prog1", itemsZapis, false);
            DataKOdeslani.Prog2 = new ItemUInt16("Z215060_8.S7-300.Pc2Plc.Prog2", itemsZapis, false);
            DataKOdeslani.Prog3 = new ItemUInt16("Z215060_8.S7-300.Pc2Plc.Prog3", itemsZapis, false);
            DataKOdeslani.Prog4 = new ItemUInt16("Z215060_8.S7-300.Pc2Plc.Prog4", itemsZapis, false);
            DataKOdeslani.Prog5 = new ItemUInt16("Z215060_8.S7-300.Pc2Plc.Prog5", itemsZapis, false);
            DataKOdeslani.Test1Vakuum = new ItemBool("Z215060_8.S7-300.Pc2Plc.Test1Vakuum", itemsZapis, false);
            DataKOdeslani.Test2Vakuum = new ItemBool("Z215060_8.S7-300.Pc2Plc.Test2Vakuum", itemsZapis, false);
            DataKOdeslani.Test3Vakuum = new ItemBool("Z215060_8.S7-300.Pc2Plc.Test3Vakuum", itemsZapis, false);
            DataKOdeslani.Test4Vakuum = new ItemBool("Z215060_8.S7-300.Pc2Plc.Test4Vakuum", itemsZapis, false);
            DataKOdeslani.Test5Vakuum = new ItemBool("Z215060_8.S7-300.Pc2Plc.Test5Vakuum", itemsZapis, false);
            DataKOdeslani.ZakladackaTyp2 = new ItemBool("Z215060_8.S7-300.Pc2Plc.ZakladackaTyp2", itemsZapis, false);
            DataKOdeslani.SMrizkou = new ItemBool("Z215060_8.S7-300.Pc2Plc.SMrizkou", itemsZapis, false);
            DataKOdeslani.NameOfType = new ItemString("Z215060_8.S7-300.Pc2Plc.NameOfType", itemsZapis, false);*/
        }

    }
}

