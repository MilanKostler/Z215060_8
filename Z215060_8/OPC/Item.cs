//***************************************************************************************
//      VERZE 4.1 - Všechny třídy public     
//      VERZE 4.0 - ItemBit32R/W, který se komunikuje přes ItemBit32R/WContainer 
//      VERZE 2.2 - Přejmenování GetWValue() na GetWValueToClient() a Parse() na SetRValueFromClient()
//      VERZE 2.1 - Implementace rozhraní IOnVariableChanged
//      VERZE 2.0 - Write mode
//***************************************************************************************
//  Třídy, které se používají v aplikaci k namapování proměnných na OPC server.
//  Hodnota je uložena ve vlastnosti Value, kam se dostane přetypováním v metodě SetRValueFromClient()
//
//***************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using HMI.Debug;

namespace HMI
{
    /// <summary>
    /// Write mode - kdy má být hodnota ItemR zapsána na OPC server.
    /// </summary>
    public enum WMode
    {
        ///<summary>Hodnota Items se zapisuje vždy, při každém cyklickém zápisu na server</summary>
        Always,
        ///<summary>Hodnota Items se zapisuje na server jen tehdy, jestliže byla změněna její hodnota, při prvním zápisu, při zápisu po chybě, nebo pokud má OPC klient nastaveno writeAll == true</summary>
        Changed,
        ///<summary>Hodnota Items se zapisuje na server jen tehdy, jestliže do ní bylo přiřazeno (zavolán setter vlastnosti Value)</summary>
        Assigned
    };


    /// <summary>
    /// Bit Container dává kolekci ItemBitů, které jdou v něm obsaženy
    /// </summary>
    interface IBitContainer
    {
        // Vrátí pole ItemBitů, které kontejner obsahuje
        Item[] GetBitsArray();
    }


    #region Bázové třídy

    /// <summary>
    /// OPC Item - bázová třída pro OPC Items růyzných typů, pro čtení, nebo zápis
    /// </summary>
    public abstract class Item : IOnVariableChanged
    {
        protected string name;
        /// <summary>
        /// ItemName - ID každé proměnné na OPC serveru
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Událost, že došlo ke změně hodnoty PLC proměnné (nebo k 1. načtení). varValue je nová hodnota proměnné. (implementace IOnVariableChanged)
        /// </summary>
        public event ChangeValueHandler OnChangeValue;

        /// <summary>
        /// Vyvolá událost, že se změnila hodnota PLC proměnné (implementace IOnVariableChanged)
        /// </summary>
        /// <param name="varValue">Aktuální hodnota nové proměnné</param>
        protected void changeValueTrigger(object varValue)
        {
            if (OnChangeValue != null)
                OnChangeValue(varValue);
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt
        /// </summary>
        /// <returns></returns>
        public abstract object GetValue();


        /// <summary>
        /// V DoubleWordu prohodí pořatí bytů (1->4, 2->3, 3->2, 4->1). Využívají to někteté zděděné tříty, které pracují s UInt32
        /// </summary>
        /// <param name="dw"></param>
        /// <returns></returns>
        protected UInt32 swapDw(UInt32 dw)
        {
            return (dw & 0x000000FFU) << 24 | (dw & 0x0000FF00U) << 8 | (dw & 0x00FF0000U) >> 8 | (dw & 0xFF000000U) >> 24;  //Suffix U znamená unsigned
        }
    }



    /// <summary>
    /// OPC Item pro čtení ze serveru - bázová třída pro OPC Items růyzných typů
    /// </summary>
    public abstract class ItemR : Item
    {
        protected DateTime? timeStamp = null;
        /// <summary>
        /// Časová známka - kdy OPC server tuto hodnotu načetl. (Pouze u OPC items pro čtení, jinak je null) 
        /// </summary>
        public DateTime? TimeStamp { get { return timeStamp; } }

        protected bool firstRead = true; //informace, že tato proměnná ještě nebyla načtena z OPC serveru (nebyla zavolána metoda SetRValueFromClient)

        public ItemR(string itemName, Dictionary<string, ItemR> hashTbl)
        {
            this.name = itemName;
            if (hashTbl != null)
                hashTbl[name] = this;
        }

        /// <summary>
        /// Metoda, kterou volá OPC klient po přijetí dat ze serveru a která definuje, jak se mají uložit data do Item.Value dle jejího typu
        /// </summary>
        /// <param name="objItem"></param>
        /// <param name="timeStamp"></param>
        public abstract void SetRValueFromClient(Object objItem, DateTime timeStamp);
    }



    /// <summary>
    /// OPC Item pro zápis na server - bázová třída pro OPC Items růyzných typů
    /// </summary>
    public abstract class ItemW : Item
    {
        protected WMode writeMode = WMode.Always;
        /// <summary>
        /// Kdy má být hodnota ItemR zapsána na OPC server - vždy, pokud je do ní přiřazeno, nebo pokud se změní.
        /// Defaultně je nastaveno Always
        /// </summary>
        public virtual WMode WriteMode
        {
            get { return writeMode; }
            set
            {
                if (writeMode != value)
                {
                    writeMode = value;
                    if (writeMode == WMode.Assigned)
                        shouldWrite = false;
                }
            }
        }

        protected bool shouldWrite;
        /// <summary>
        /// Informace pro OpcClienta, zda má tuto ItemW v příštím zapisovacím cyklu zapsat na OPC server
        /// </summary>
        public virtual bool ShouldWrite
        {
            get
            {
                return shouldWrite || (WriteMode == WMode.Always);
            }
        }

        public ItemW(string itemName, Dictionary<string, ItemW> hashTbl)
        {
            name = itemName;
            shouldWrite = true;
            if (hashTbl != null)
                hashTbl[name] = this;
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Metoda vrací hodnotu proměnné, která se bude zapisovat na OPC server. 
        /// Tuto metodu smí používat jen OPC client, protoře se při ní mění vlastnost ShouldWrite!
        /// </summary>
        /// <returns></returns>
        public abstract Object GetWValueToClient();
    }

    #endregion Bázové třídy


    #region Bool

    /// <summary>
    /// OPC Item typu bool pro čtení ze serveru
    /// </summary>
    public class ItemBoolR : ItemR
    {
        private bool value;
        /// <summary>
        /// Hodnota proměnné
        /// </summary>
        public bool Value
        {
            get { return this.value; }
        }

        /// <summary>
        /// Náběžná hrana
        /// </summary>
        public bool EU
        {
            get
            {
                return (value && !lastValue);
            }
        }

        /// <summary>
        /// Sestupná hrana
        /// </summary>
        public bool ED
        {
            get
            {
                return (!value && lastValue);
            }
        }

        private bool lastValue;
        /// <summary>
        /// Hodnota proměnné v minulém cyklu komunikace
        /// </summary>
        public bool LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemBoolR(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            bool temp = value;
            value = (bool)objItem;
            this.timeStamp = timeStamp;
            if ((value != temp) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu bool pro zápis na server
    /// </summary>
    public class ItemBoolW : ItemW
    {
        private bool value;
        /// <summary>
        /// Hodnota proměnné
        /// </summary>
        public bool Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemBoolW(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }


    /// <summary>
    /// OPC Item typu bit v UInt32 pro čtení ze serveru. Sám si vytvoří instanci ItemBit32RContainer, přes který se pak načítá až 32 bitů z PLC najednou.
    /// </summary>
    public class ItemBit32R : ItemBoolR
    {
        private int cisloBitu;
        /// <summary>
        /// Pořadové číslo bitu v UInt32 (0..31)
        /// </summary>
        public int CisloBitu
        {
            get { return cisloBitu; }
            set
            {
                if ((value < 0) || (value > 31))
                    throw new ArgumentOutOfRangeException("ItemBit32R.CisloBitu musí být v rozmezí 0..31");
                cisloBitu = value;
            }
        }

        /// <summary>
        /// Zda se mají při zápise prohazovat byty ItemBit32RContaineru, který obsahuje tyto bity
        /// </summary>
        public void SetContainersSwapBytes(bool swapBytes)
        {
            container.SwapBytes = swapBytes;
        }

        //Reference na ItemBit32RContaineru, který obsahuje tuto instanci
        private ItemBit32RContainer container;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">ID název UInt32 proměnné na OPC serveru, která bude soužit jako kontejner pro přenos bitů </param>
        /// <param name="hashTbl">Hashtabulka, do které se přidá ItemBit32RContainer. Instance tohoto ItemBitu se do hashTbl nepřidává.</param>
        /// <param name="bitName">Název ItemBitu. Neslouží jako ID pro komunikaci jako je tomu u ostatních OPCItems, nýbrž jako identifikace při ladění a zobrazování.</param>
        /// <param name="cisloBitu">Číslo bitu v ItemBit32RContaineru (0..31)</param>
        public ItemBit32R(string id, Dictionary<string, ItemR> hashTbl, string bitName, int cisloBitu) :
            base(bitName, null)
        {
            CisloBitu = cisloBitu;
            ItemBit32RContainer itemBits32RContainer;
            if (hashTbl.ContainsKey(id))
                itemBits32RContainer = hashTbl[id] as ItemBit32RContainer;
            else
                itemBits32RContainer = new ItemBit32RContainer(id, hashTbl);

            if (itemBits32RContainer == null)
                throw new Exception("OPC item typu ItemBit32R se parametrem name odkazuje na jinou třídu než na ItemBit32RContainer");

            container = itemBits32RContainer;
            container.AddBit(this);
        }
    }


    /// <summary>
    /// OPC Item typu bit v UInt32 pro zápis na server. Sám si vytvoří instanci ItemBit32wContainer, přes který se pak zapisuje až 32 bitů do PLC najednou.
    /// </summary>
    public class ItemBit32W : ItemBoolW
    {
        /// <summary>
        /// Kdy má být hodnota ItemR zapsána na OPC server - vždy, nebo pokud se změní. (Nelze nastavit WriteMode.Assigned)
        /// Nasrtavením WriteMode se nastaví WriteMode jeho containeru (ItemBit32WContainer)
        /// Defaultně je nastaveno Always
        /// </summary>
        public override WMode WriteMode
        {
            get { return container.WriteMode; }
            set
            {
                container.WriteMode = value;
            }
        }

        private int cisloBitu;
        /// <summary>
        /// Pořadové číslo bitu v UInt32 (0..31)
        /// </summary>
        public int CisloBitu
        {
            get { return cisloBitu; }
            set
            {
                if ((value < 0) || (value > 31))
                    throw new ArgumentOutOfRangeException("ItemBit32W.CisloBitu musí být v rozmezí 0..31");
                cisloBitu = value;
            }
        }

        /// <summary>
        /// Zda se mají při zápise prohazovat byty ItemBit32WContaineru, který obsahuje tyto bity
        /// </summary>
        public void SetContainersSwapBytes(bool swapBytes)
        {
            container.SwapBytes = swapBytes;
        }

        //Reference na ItemBit32RContaineru, který obsahuje tuto instanci
        private ItemBit32WContainer container;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">ID název UInt32 proměnné na OPC serveru, která bude soužit jako kontejner pro přenos bitů </param>
        /// <param name="hashTbl">Hashtabulka, do které se přidá ItemBit32WContainer. Instance tohoto ItemBitu se do hashTbl nepřidává.</param>
        /// <param name="bitName">Název ItemBitu. Neslouží jako ID pro komunikaci jako je tomu u ostatních OPCItems, nýbrž jako identifikace při ladění a zobrazování.</param>
        /// <param name="cisloBitu">Číslo bitu v ItemBit32WContaineru (0..31)</param>
        public ItemBit32W(string id, Dictionary<string, ItemW> hashTbl, string bitName, int cisloBitu) :
            base(bitName, null)
        {
            CisloBitu = cisloBitu;
            ItemBit32WContainer itemBits32WContainer;
            if (hashTbl.ContainsKey(id))
                itemBits32WContainer = hashTbl[id] as ItemBit32WContainer;
            else
                itemBits32WContainer = new ItemBit32WContainer(id, hashTbl);

            if (itemBits32WContainer == null)
                throw new Exception("OPC item typu ItemBit32W se parametrem name odkazuje na jinou třídu než na ItemBit32WContainer");

            container = itemBits32WContainer;
            container.AddBit(this);
        }
    }

    #endregion Bool


    #region Bit containers

    /// <summary>
    /// OPC Item, která slouží jako kontejner pro načtení bitů ItemBit32R ze serveru najednou přes UInt32.
    /// </summary>
    public class ItemBit32RContainer : ItemR, IBitContainer
    {
        /// <summary>
        /// Hodnota integeru, do kterého se zapisují bity konteineru
        /// </summary>
        private UInt32 value;

        /// <summary>
        /// pole itemBitů, které kontejner může obsahovat
        /// </summary>
        private ItemBit32R[] bits = new ItemBit32R[32];

        private bool swapBytes;
        /// <summary>
        /// Zda se mají při zápise a čtení prohazovat byty DWordu. Swapování se projeví až při načtení z OPC serveru.
        /// </summary>
        public bool SwapBytes
        {
            get { return swapBytes; }
            set
            {
                if (swapBytes != value)
                {
                    swapBytes = value;
                }
            }
        }

        public ItemBit32RContainer(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public void AddBit(ItemBit32R bit)
        {
            if (bits[bit.CisloBitu] != null)
                throw new Exception(String.Format("OPC Item typu ItemBit32RContainer má již {0}tý bit obsazen.", bit.CisloBitu));
            else
                bits[bit.CisloBitu] = bit;
        }

        /// <summary>
        /// Vrátí pole ItemBitů, které kontejner obsahuje (implementace IBitContainer)
        /// </summary>
        /// <returns></returns>
        public Item[] GetBitsArray()
        {
            List<ItemBit32R> list = new List<ItemBit32R>();
            foreach (ItemBit32R bit in bits)
                if (bit != null)
                    list.Add(bit);
            return list.ToArray();
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            UInt32 lastValue = value;
            value = (UInt32)objItem;
            if (SwapBytes)
                value = swapDw(value);
            this.timeStamp = timeStamp;
            setRValuesFromValue();
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
        }

        /// <summary>
        /// Vypreparování jednotlivých bitů z value a nastavení jednotlivých ItemBitů
        /// </summary>
        private void setRValuesFromValue()
        {
            foreach (ItemBit32R bit in bits)
                if (bit != null)
                {
                    bool newVal = ((value & (1 << bit.CisloBitu)) != 0);
                    bit.SetRValueFromClient(newVal, timeStamp.Value);
                }
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 31; i >= 0; i--)
            {
                if (bits[i] != null)
                    sb.Append(bits[i].Value ? "1" : "0");
                else
                    sb.Append("-");
                if ((i == 24) || (i == 16) || (i == 8))
                    sb.Append(" ");
            }
            return sb.ToString();
        }
    }


    /// <summary>
    /// OPC Item, která slouží jako kontejner pro zális bitů ItemBit32w na server najednou přes UInt32.
    /// </summary>
    public class ItemBit32WContainer : ItemW, IBitContainer
    {
        /// <summary>
        /// Hodnota integeru, do kterého se zapisují bity konteineru
        /// </summary>
        private UInt32 value;

        /// <summary>
        /// Naposledy zapsaná
        /// </summary>
        // private UInt32 lastWritedValue;  smazat!!!

        /// <summary>
        /// Kdy má být hodnota ItemR zapsána na OPC server - vždy, nebo pokud se změní.
        /// ItemBit32WContainer nemůže mít WMode.Assigned, to by bylo dosti matoucí chování.
        /// </summary>
        public override WMode WriteMode
        {
            get { return writeMode; }
            set
            {
                if (writeMode != value)
                {
                    if (value == WMode.Assigned)
                        throw new ArgumentException("ItemBit32WContainer nemůže mít writeMode == WMode.Assigned");
                    writeMode = value;
                }
            }
        }

        /// <summary>
        /// pole itemBitů, které kontejner může obsahovat
        /// </summary>
        private ItemBit32W[] bits = new ItemBit32W[32];

        private bool swapBytes;
        /// <summary>
        /// Zda se mají při zápise a čtení prohazovat byty DWordu
        /// </summary>
        public bool SwapBytes
        {
            get { return swapBytes; }
            set
            {
                swapBytes = value;
                shouldWrite = true;
                setValueFromBits();
            }
        }

        public ItemBit32WContainer(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        public void AddBit(ItemBit32W bit)
        {
            if (bits[bit.CisloBitu] != null)
                throw new Exception(String.Format("OPC Item typu ItemBit32WContainer má již {0}tý bit obsazen.", bit.CisloBitu));

            bits[bit.CisloBitu] = bit;
            bit.OnChangeValue += (varValue) =>
            {
                shouldWrite = true;
                setValueFromBits();
                changeValueTrigger(value);
            };
        }

        /// <summary>
        /// Vrátí pole ItemBitů, které kontejner obsahuje (implementace IBitContainer)
        /// </summary>
        /// <returns></returns>
        public Item[] GetBitsArray()
        {
            List<ItemBit32W> list = new List<ItemBit32W>();
            foreach (ItemBit32W bit in bits)
                if (bit != null)
                    list.Add(bit);
            return list.ToArray();
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            setValueFromBits();
            return value;
        }

        /// <summary>
        /// Nastavení znovu value dle jednotlivých ItemBitů v kolekci
        /// </summary>
        private void setValueFromBits()
        {
            UInt32 newVal = 0;
            foreach (ItemBit32W bit in bits)
                if (bit != null)
                    if (bit.Value)
                        newVal |= (0x1U << bit.CisloBitu);
            if (SwapBytes)
                value = swapDw(newVal);
            else
                value = newVal;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 31; i >= 0; i--)
            {
                if (bits[i] != null)
                    sb.Append(bits[i].Value ? "1" : "0");
                else
                    sb.Append("-");
                if ((i == 24) || (i == 16) || (i == 8))
                    sb.Append(" ");
            }
            return sb.ToString();
        }
    }

    #endregion Bit containers


    #region Int

    /// <summary>
    /// OPC Item typu int32 pro čtení ze serveru
    /// </summary>
    public class ItemInt32R : ItemR
    {
        private int value;
        public int Value
        {
            get { return this.value; }
        }

        private int lastValue;
        public int LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemInt32R(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            int temp = value;
            value = (int)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }


    /// <summary>
    /// OPC Item typu int32 pro zápis na server
    /// </summary>
    public class ItemInt32W : ItemW
    {
        private int value;
        public int Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemInt32W(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }


    /// <summary>
    /// OPC Item typu int16 pro čtení ze serveru
    /// </summary>
    public class ItemInt16R : ItemR
    {
        private Int16 value;
        public Int16 Value
        {
            get { return this.value; }
        }

        private int lastValue;
        public int LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemInt16R(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            int temp = value;
            value = (Int16)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu int16 pro zápis na server
    /// </summary>
    public class ItemInt16W : ItemW
    {
        private Int16 value;
        public Int16 Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemInt16W(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu UInt32 (v PLC DWord) pro čtení ze serveru
    /// </summary>
    public class ItemUInt32R : ItemR
    {
        private UInt32 value;
        public UInt32 Value
        {
            get { return this.value; }
        }

        private UInt32 lastValue;
        public UInt32 LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        /// <summary>
        /// Zda se mají při zápise a čtení prohazovat byty DWordu
        /// </summary>
        public bool SwapBytes { get; set; }

        public ItemUInt32R(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            UInt32 temp = value;
            value = (UInt32)objItem;
            if (SwapBytes)
                value = swapDw(value);
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu UInt32 (v PLC DWord) pro zápis na server
    /// </summary>
    public class ItemUInt32W : ItemW
    {
        private UInt32 value;
        public UInt32 Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        /// <summary>
        /// Zda se mají při zápise a čtení prohazovat byty DWordu
        /// </summary>
        public bool SwapBytes { get; set; }

        public ItemUInt32W(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            if (SwapBytes)
                return swapDw(Value);
            else
                return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu UInt16 (v PLC Word) pro čtení ze serveru
    /// </summary>
    public class ItemUInt16R : ItemR
    {
        private UInt16 value;
        public UInt16 Value
        {
            get { return this.value; }
        }

        private UInt16 lastValue;
        public UInt16 LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemUInt16R(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            UInt16 temp = value;
            value = (UInt16)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu UInt16 (v PLC Word) pro zápis na server
    /// </summary>
    public class ItemUInt16W : ItemW
    {
        private UInt16 value;
        public UInt16 Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemUInt16W(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu byte pro čtení ze serveru
    /// </summary>
    public class ItemByteR : ItemR
    {
        private Byte value;
        public Byte Value
        {
            get { return this.value; }
        }

        private Byte lastValue;
        public Byte LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemByteR(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            Byte temp = value;
            value = (Byte)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu byte pro zápis na server
    /// </summary>
    public class ItemByteW : ItemW
    {
        private Byte value;
        public Byte Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemByteW(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }


    #endregion Int


    #region Float

    /// <summary>
    /// OPC Item typu double pro čtení ze serveru (v PLC je 4 bytový, tj. jako single v PC)
    /// </summary>
    public class ItemDoubleR : ItemR
    {
        private Double value;
        public Double Value
        {
            get { return this.value; }
        }

        private Double lastValue;
        public Double LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemDoubleR(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            Double temp = value;
            value = (Double)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu double pro zápis na server (v PLC je 4 bytový, tj. jako single v PC)
    /// </summary>
    public class ItemDoubleW : ItemW
    {
        private Double value;
        public Double Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemDoubleW(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }

    #endregion Float


    #region String

    /// <summary>
    /// OPC Item typu string pro čtení ze serveru
    /// </summary>
    public class ItemStringR : ItemR
    {
        private string value = "";
        public string Value
        {
            get { return this.value; }
        }

        private string lastValue;
        public string LastValue { get { return lastValue; } }

        /// <summary>
        /// Změna hodnoty
        /// </summary>
        public bool Changed
        {
            get
            {
                return (value != lastValue);
            }
        }

        public ItemStringR(string id, Dictionary<string, ItemR> hashTbl) :
            base(id, hashTbl)
        {
        }

        public override void SetRValueFromClient(Object objItem, DateTime timeStamp)
        {
            string temp = value;
            value = (string)objItem;
            this.timeStamp = timeStamp;
            if ((value != lastValue) || (firstRead))
                changeValueTrigger(value);
            firstRead = false;
            lastValue = temp;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }



    /// <summary>
    /// OPC Item typu string pro zápis na server. 
    /// Na OPC server se zapisuje prázdný string i string s diakritikou, do PLC se pak (v závislosti na typu PLC) přenáší jen ASCII.
    /// Pokud je Value prázdný řetězec, data se do PLC nezapisují, je proto nutné posílat např. mezeru.
    /// </summary>
    public class ItemStringW : ItemW
    {
        private string value = "";
        public string Value
        {
            get { return this.value; }
            set
            {
                if (WriteMode == WMode.Assigned)
                    shouldWrite = true;
                if (this.value != value)
                {
                    if (WriteMode == WMode.Changed)
                        shouldWrite = true;
                    this.value = value;
                    changeValueTrigger(value);
                }
            }
        }

        public ItemStringW(string id, Dictionary<string, ItemW> hashTbl) :
            base(id, hashTbl)
        {
        }

        /// <summary>
        /// Metoda, kterou volá OPC client při zápisu. Nikdo jiný by jí neměl volat!
        /// </summary>
        /// <returns></returns>
        public override Object GetWValueToClient()
        {
            shouldWrite = false;
            return Value;
        }

        /// <summary>
        /// Vrátí hodnotu Value jako objekt (implementace IOnVariableChanged)
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }
    }


    #endregion String
}
