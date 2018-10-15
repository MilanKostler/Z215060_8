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
using Sablona;
using LeakTesty;

namespace Z215060_8
{
    /// <summary>
    /// Interaction logic for ucVizualizace.xaml
    /// </summary>
    public partial class ucVizualizace : UserControl
    {
        private StavStrojeViewModel stavStrojeVM;
        /// <summary>
        /// View model stavu stroje (TOTAL STOP, READY...)
        /// </summary>
        public StavStrojeViewModel StavStrojeVM
        {
            get { return stavStrojeVM; }
            set
            {
                stavStrojeVM = value;
                lblStav.DataContext = stavStrojeVM;
            }
        }


        public ucVizualizace()
        {
            InitializeComponent();

            //Nastavení DataContextů udělat jinde !!!
            /*Machine stroj = VizualizaceZ215060_8.Instance.Stroj;

            lblIddA.DataContext = stroj.PTC04;
            lblIddB.DataContext = stroj.PTC04;
            lblOutA.DataContext = stroj.PTC04;
            lblOutB.DataContext = stroj.PTC04; */
        }


        /// <summary>
        /// Zobrazení výrobního procesu (hodnot, které se nezobrazují jako properties objektu Stroj)
        /// </summary>
        /// <param name="opcClient"></param>
        public void ZobrazitProces(TPrijataData prijataData)
        {
            ucVizuL.zobrazitPokyn(prijataData.L.Message.Value);
            ucVizuP.zobrazitPokyn(prijataData.P.Message.Value);
        }
    }

}
