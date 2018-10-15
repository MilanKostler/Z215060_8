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
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Interaction logic for WindowShutdown.xaml
    /// </summary>
    public partial class WindowShutdown : Window
    {
        private DispatcherTimer timerOdpocet;
        private int zbyvaSekund;

        public int ZbyvaSekund
        {
            get { return zbyvaSekund; }
            set 
            { 
                zbyvaSekund = value;
                labelOdpocet.Content = zbyvaSekund.ToString();
            }
        }

            //Deklarace událostí
        public delegate bool TestKomunikaceHandler();
        public event TestKomunikaceHandler OnTestKomunikace;

        public WindowShutdown(int pocetSekund)
        {
            InitializeComponent();

            timerOdpocet = new DispatcherTimer();
            timerOdpocet.Interval = TimeSpan.FromMilliseconds(933); //při intervalu 1000 ms se za minutu zpozdí o cca 4s
            timerOdpocet.Tick += new EventHandler(timerOdpocet_Tick);
            ZbyvaSekund = pocetSekund;
        }

        void timerOdpocet_Tick(object sender, EventArgs e)
        {
            ZbyvaSekund--;
            if (OnTestKomunikace != null)
                if (OnTestKomunikace())
                    this.DialogResult = false;
            if (ZbyvaSekund == 0)
                this.DialogResult = true;
        }

        //Zastavení odpočtu
        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //Start odpočtu
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timerOdpocet.Start();
        }

        //Zastavení odpočtu
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timerOdpocet.Stop();
        }

    }
}
