using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для ColdReset.xaml
    /// </summary>
    public partial class ColdReset : Window
    {
        public ColdReset()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Подтверждение сброса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Aplly_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Отмена сброса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
