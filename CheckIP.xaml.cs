using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace checkmod
{
    /// <summary>
    /// Логика взаимодействия для CheckIP.xaml
    /// </summary>
    public partial class CheckIP : Window
    {
        public CheckIP()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Добавляет IP на выбранный сетевой адаптер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddIPAddress(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            // Присваиваем имя выбранного адаптера 
            for (int i = 0; i < HeaderDriver.list_adapters.Length; i++)
                if (HeaderDriver.list_adapters[i].Name == NI.Text)
                    HeaderDriver.select_network_adapter = HeaderDriver.list_adapters[i].Name;

            Close();
        }

        /// <summary>
        /// Завершает работу приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseApp(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HeaderDriver.f_save_ip = !HeaderDriver.f_save_ip;
        }
    }
}
