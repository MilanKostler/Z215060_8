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
using System.Windows.Shapes;

namespace Z215060_8
{
    [Flags]
    public enum Strana
    {
        Zadna = 0x00,
        Leva = 0x01,
        Prava = 0x02,
    }


    /// <summary>
    /// Interaction logic for WinVyberStrany.xaml
    /// </summary>
    public partial class WinVyberStrany : Window
    {
        /// <summary>
        /// Uživatelem zvolená strana
        /// </summary>
        public Strana Vyber = Strana.Zadna;

        public WinVyberStrany()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Vytvoří dialok dotazu na stranu a vrátí uživatelovu volbu pomoví enum Strana
        /// </summary>
        public static Strana VyberStrany(bool povolitObeStrany)
        {
            WinVyberStrany wvs = new WinVyberStrany();
            wvs.btnObe.IsEnabled = povolitObeStrany;
            wvs.ShowDialog();
            return wvs.Vyber;
        }


        //Reakce na stisk tlačítka
        private void btnLeva_Click(object sender, RoutedEventArgs e)
        {
            Vyber = Strana.Leva;
            this.Close();
        }


        //Reakce na stisk tlačítka
        private void btnPrava_Click(object sender, RoutedEventArgs e)
        {
            Vyber = Strana.Prava;
            this.Close();
        }


        //Reakce na stisk tlačítka
        private void btnObe_Click(object sender, RoutedEventArgs e)
        {
            Vyber |= Strana.Leva;
            Vyber |= Strana.Prava;
            this.Close();
        }
    }
}
