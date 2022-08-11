using System;
using System.Collections.Generic;
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

namespace FTDI.View
{
    /// <summary>
    /// Логика взаимодействия для Connect.xaml
    /// </summary>
    public partial class Connect : UserControl 
    {  
        public Connect()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //can = new UCan();
            //can.Start(0, 250);
            //Program.DO();
            FTDI_Hardware fTDI = new FTDI_Hardware();
            fTDI.Connect();


        }
    }
}
