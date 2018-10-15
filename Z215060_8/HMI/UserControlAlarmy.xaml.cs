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
using System.Collections;
using System.Globalization;

namespace HMI
{
    /// <summary>
    /// UserControl pro zobrazování alarmů
    /// </summary>
    public partial class UserControlAlarmy : UserControl
    {
        public UserControlAlarmy()
        {
            InitializeComponent();
        }

        //Zbavení se focusu
        private void listBoxAlarmy_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;
        }


        /// <summary>
        /// Přiřadí zdroj dat listboxu (kolekci aktivních alarmů)
        /// </summary>
        /// <param name="itemSource"></param>
        public void SetListBoxItemSource(IEnumerable itemSource)
        {
            listBoxAlarmy.ItemsSource = itemSource;
        }
    }


    /// <summary>
    /// Převede boolovskou informaci na viditelnost takto: true..Visible, false..collapsed
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = (bool)value;
            if (visible)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Převodník viditelnosti na bool není implementován");
        }
    }
}
