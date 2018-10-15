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
using System.Data.OleDb;
using System.Data;
using System.Diagnostics;

namespace HMI
{
    /// <summary>
    /// Interaction logic for WindowLogin.xaml
    /// </summary>
    public partial class WinLogin : Window
    {
        public WinLogin()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Zobrazí dialog pro zadání hesla. V případě, že dialogresult není true (uživatel si zadání hesla rozmyslel), vrátí null. Jinak vrátí zadané heslo.
        /// </summary>
        /// <returns></returns>
        public static string GetHeslo()
        {
            WinLogin wl = new WinLogin();
            wl.ShowDialog();
            if (wl.DialogResult.Value)
                return wl.passwordBoxHeslo.Password;
            else
                return null;
        }
        
        
        //Reakce na stisk tlačítka "Přihlášení"
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }


        // Reakce na stisk klávesy
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.DialogResult = true;
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
        }


        //je-li při otevření okna v databázi jediný uživatel, který není superuser, automaticky se vyplní jeho jméno 
        //a nastaví se focus na passwordBoxHeslo. 
        private void Window_Activated(object sender, EventArgs e)
        {
            passwordBoxHeslo.Clear();
            passwordBoxHeslo.Focus();
        }


        //Zobrazení SW klávesnice
        private void btnKeyboad_Click(object sender, RoutedEventArgs e)
        {
            passwordBoxHeslo.Focus();
            Process[] p = Process.GetProcessesByName("osk");
            if (p.Length == 0)
                Process.Start("osk.exe");
        }


        //Zavření SW klávesnice
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                Process[] p = Process.GetProcessesByName("osk");
                foreach (var item in p)
                    item.Kill();
            }
            catch { }
        }
    }
}
