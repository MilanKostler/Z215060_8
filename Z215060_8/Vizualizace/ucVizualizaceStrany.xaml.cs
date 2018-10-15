using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HMI;

namespace Z215060_8
{
    /// <summary>
    /// Interaction logic for ucVizualizaceStrany.xaml
    /// </summary>
    public partial class ucVizualizaceStrany : UserControl
    {
        private PocitadloViewModel pocitadloVM;
        /// <summary>
        /// View model počítadla OK NOK Celkem
        /// </summary>
        public PocitadloViewModel PocitadloVM
        {
          get { return pocitadloVM; }
          set 
          { 
              pocitadloVM = value;
              labelVyrobeno.DataContext = pocitadloVM;
              labelOK.DataContext = pocitadloVM;
              labelNOK.DataContext = pocitadloVM;
          }
        }


        public ucVizualizaceStrany()
        {
            InitializeComponent();

            lblPokyn.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Zobrazení pokynu obsluze
        /// </summary>
        /// <param name="message"></param>
        public void zobrazitPokyn(int pokyn)
        {
            switch (pokyn)
            {
                case 1:
                    lblPokyn.Content = "Pokud je plc v módu run, vyčkejte, až se dokončí start";
                    break;
                case 2:
                    lblPokyn.Content = "Odaretujte všechna tlačítka nouzové zaststavení a stiskněte tlačítko inicializace";
                    break;
                case 3:
                    lblPokyn.Content = "Stroj se zapíná, vyčkejte až doběhne inicializace";
                    break;
                case 4:
                    lblPokyn.Content = "Přes menu otevřte ruč. ovládání, nebo otočte klíčkem do auto";
                    break;
                case 15:
                    lblPokyn.Content = "Odstraňte poruchy";
                    break;
                case 18:
                    lblPokyn.Content = "Odsraňte poruchy / Proveďte homing tlačítkem reset";
                    break;
                case 19:
                    lblPokyn.Content = "Proveďte homing tlačítkem reset";
                    break;
                case 20:
                    lblPokyn.Content = "Připojte zakladačku";
                    break;
                case 21:
                    lblPokyn.Content = "Založte výrobek";
                    break;
                case 22:
                    lblPokyn.Content = "Opusťte prostor optických závor";
                    break;
                case 23:
                    lblPokyn.Content = "Spusťte test tlačítkem start znovu, nebo výrobek odeberte";
                    break;
                case 24:
                    lblPokyn.Content = "Spusťte test tlačítkem start";
                    break;
                case 25:
                    lblPokyn.Content = "Odeberte výrobek";
                    break;
                case 101:
                    lblPokyn.Content = "Pokud je plc v módu run, vyčkejte, až se dokončí start";
                    break;
                case 103:
                    lblPokyn.Content = "Stroj se zapíná, vyčkejte až doběhne inicializace";
                    break;
                case 110:
                    lblPokyn.Content = "Test je blokován nadřazeným systémem VREOS";
                    break;
                case 120:
                    lblPokyn.Content = "Vyčkejte až skončí test";
                    break;
                case 121:
                    lblPokyn.Content = "Vyčkejte až skončí homing";
                    break;

                default:
                    lblPokyn.Content = "";
                    break;
            }
            lblPokyn.Visibility = Visibility.Visible;
        }
    }
}
