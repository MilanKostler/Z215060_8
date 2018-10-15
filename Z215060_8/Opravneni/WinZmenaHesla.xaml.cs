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

namespace HMI
{
    /// <summary>
    /// Interaction logic for WindowZmenaHesla.xaml
    /// </summary>
    public partial class WinZmenaHesla : Window
    {
        public delegate bool PasswordOkHandler(string password);
        public event PasswordOkHandler OnPasswordOk;

        public WinZmenaHesla()
        {
            InitializeComponent();
        }



        //Ověření zadaných hodnot (původní heslo, shoda 2 polí pro zadání nového hesla a délka nového hesla)
        private bool validace()
        {
            if (OnPasswordOk == null)
                throw new Exception("WinZmenaHesla: Nadřazený objekt neinplementuje handler OnPasswordOk");

            if (!OnPasswordOk(passwordBoxSoucasneHeslo.Password))
            {
                MessageBox.Show("Nesprávné současné heslo!");
                passwordBoxSoucasneHeslo.Focus();
                passwordBoxSoucasneHeslo.SelectAll();
                return false;
            }
            if (passwordBoxNoveHeslo1.Password != passwordBoxNoveHeslo2.Password)
            {
                MessageBox.Show("Špatně zadané nové heslo!\nPoložky \"" + labelNoveHeslo1.Content + "\" a \"" + labelNoveHeslo2.Content + "\" se neshodují.");
                passwordBoxNoveHeslo1.Clear();
                passwordBoxNoveHeslo2.Clear();
                passwordBoxNoveHeslo1.Focus();
                //passwordBoxNoveHeslo1.SelectAll();
                return false;
            }
            int minDelkaHesla = 4;
            if (passwordBoxNoveHeslo1.Password.Length < minDelkaHesla)
            {
                MessageBox.Show("Špatně zadané nové heslo!\nMinimální počet znaků hesla je " + minDelkaHesla.ToString() + ".");
                passwordBoxNoveHeslo1.Clear();
                passwordBoxNoveHeslo2.Clear();
                passwordBoxNoveHeslo1.Focus();
                return false;
            }

            return true;
        }


        //Uložení nového hesla do databáze
        private void ulozitHelso()
        {
            if (validace())
            {
                /*using (OleDbConnection OleConn = new OleDbConnection())
                {
                    //Připojení k databázi
                    OleConn.ConnectionString = ConnectionStringZacatek + strDatabaze + ConnectionStringKonec;
                    OleConn.Open();  //Aby toto fungovalo, musí být Microsoft.ACE.OLEDB.12.0 buďto 32bit on 32bit target machine, nebo 64bit on 64bit target machine, nebo musí být Platform target aplikace x86 namísto any CPU

                    string strUpdateSql = "UPDATE Hesla SET Heslo = @Heslo";
                    try
                    {
                        OleDbCommand cmd = new OleDbCommand(strUpdateSql, OleConn);

                        cmd.Parameters.Add("@Heslo", OleDbType.VarChar, 50, "Heslo");
                        cmd.Parameters["@Heslo"].Value = passwordBoxNoveHeslo1.Password;
                        cmd.ExecuteNonQuery();  //Zápis do databáze
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Nové heslo se nepodařilo uložit do databáze\n\n" + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.DialogResult = false;
                        return;
                    }
                }*/


                this.DialogResult = true;
            }
        }


        //Reakce na stisk tlačítka "OK"
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ulozitHelso();
        }


        //Po aktivaci okna se zkontroluje, zda je vůbec možná změna hesla uživatele
        private void Window_Activated(object sender, EventArgs e)
        {
            passwordBoxSoucasneHeslo.Focus();
        }


        //Reakce na stisknutí klávesy
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    ulozitHelso();
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
        }
    }
}
