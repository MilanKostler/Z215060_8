using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Z215060_8
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Globální ošetření výjimky
        void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var mv = App.Current.MainWindow as MainWindow; 
            mv.GlobalExceptionHandler(e.Exception);
            e.Handled = true;
        }

    }

}
