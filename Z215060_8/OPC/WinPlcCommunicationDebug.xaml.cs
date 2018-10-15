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

namespace HMI.Debug
{
    /// <summary>
    /// Interaction logic for WinPlcCommunicationDebug.xaml
    /// </summary>
    public partial class WinPlcCommunicationDebug : Window
    {

        private PlcVariablesViewModel plcVariablesVM;
        /// <summary>
        /// ViewModel PLC proměnných
        /// </summary>
        public PlcVariablesViewModel PlcVariablesVM
        {
            get { return plcVariablesVM; }
            set 
            {
                plcVariablesVM = value;
                listBoxRead.ItemsSource = plcVariablesVM.ReadVariables;
                listBoxWrite.ItemsSource = plcVariablesVM.WriteVariables;
                lblStatistika.DataContext = plcVariablesVM;
            }
        }


        public WinPlcCommunicationDebug()
        {
            InitializeComponent();

            ToolTipService.SetShowDuration(lblHelp, 10000);
        }


        //Zbavení se focusu
        private void listBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;
        }


        /// <summary>
        /// Reakce na klik myší. Levé tlačítko změní zobrazení, praé uloží hodnotu do schránky
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PlcVariableViewModel pvvm = ((FrameworkElement)sender).DataContext as PlcVariableViewModel;
            if (e.LeftButton == MouseButtonState.Pressed)              
                pvvm.MouseDownHandler();
            else
                Clipboard.SetDataObject(pvvm.Hodnota);
        }



        /// <summary>
        /// Shování nápovědy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lblHelp_MouseLeave(object sender, MouseEventArgs e)
        {
            ((ToolTip)((FrameworkElement)sender).ToolTip).IsOpen = false;
        }



        /// <summary>
        /// Zobrazení nápovědy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lblHelp_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ((ToolTip)((FrameworkElement)sender).ToolTip).IsOpen = true;
        }
    }
}
