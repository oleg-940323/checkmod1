using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Класс, описывающий параметр приложения
    /// </summary>
    public class s_param
    {
        public s_param(string name, int val, string measur, string type)
        {
            this.name = name;
            this.val = val;
            this.type = type;
            this.measur = measur;
            max = int.MaxValue;
            min = int.MinValue;
        }

        // Имя параметра
        public string name { get; set; }

        // Значение параметра
        public int val { get; set; }

        // Минимальное значение параметра
        public int min { get; set; }

        // Максимальное значение параметра
        public int max { get; set; }

        // Еденица измерения
        public string measur { get; set; }

        // Тип параметра для приложения
        public string type { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для Params.xaml
    /// </summary>
    public partial class Params : Window, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Коллекция параметров
        public ObservableCollection<s_param> common_parameters
        {
            get { return HeaderDriver._common_parameters; }
            set { HeaderDriver._common_parameters = value; }
        }

        public Params()
        {
            InitializeComponent();
            Parameters.DataContext = this;
            OnPropChanged("common_parameters");
        }

        // Применить действия
        private void Aplly_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            ChangeValue();
            Close();
        }

        // Отменить действия
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Изменить таймауты таймеров
        /// </summary>
        public void ChangeValue()
        {
            // Контейнер для параметра
            s_param t = null;

            // перебор параметров приложения
            for (int i = 0; i < common_parameters.Count; i++)
            {
                // Временный контейнер таймера
                System.Timers.Timer tempr_timer;

                //Проверка нахождения таймера в словаре по имени типа 
                if (HeaderDriver.params_for_driver.ContainsKey(common_parameters[i].type))
                {
                    // Получение таймера
                    HeaderDriver.params_for_driver.TryGetValue(common_parameters[i].type, out tempr_timer);

                    // Изменение значения
                    if (tempr_timer.Enabled)
                    {
                        tempr_timer.Stop();
                        tempr_timer.Interval = common_parameters[i].val;
                        tempr_timer.Start();
                    }
                    else
                        tempr_timer.Interval = common_parameters[i].val;
                }

                // Обновление данных 
                try
                {
                    // Время паузы между переотправкой кадра по мультикасту
                    t = common_parameters.Single(x => x.type == "time_multi");
                    HeaderDriver.time_multi = t.val;

                    // Время переотправки кадра с выходными данными
                    t = common_parameters.Single(x => x.type == "retry_time");
                    HeaderDriver.retry_time = t.val;

                    // Время обновления данных в окне
                    t = common_parameters.Single(x => x.type == "change_time");
                    HeaderDriver.change_time = t.val;
                }
                catch
                { }
            }
        }
    }
}
