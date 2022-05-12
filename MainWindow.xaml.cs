﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace checkmod
{

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Интерфейс обновления данных в окне
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Драйвер ПО
        Driver dr;

        // Счетчик количества циклов
        int cnt = 0;

        // Окно с добавлением IP
        CheckIP add_IP = null;

        // Сокет приложения
        public UdpClient client;

        // Флаг выхода из бесконечных циклов
        public bool forever = true;

        // Окно с подтверждением принятия холодного сброса
        ColdReset cold_reset = null;

        // Окно с общими параметрами
        Params param = new Params();

        // Объект класса сгенерированного из XML с параметрами ПО
        Collect common_param = null;

        // Защита части кода от доступа нескольких потоков одновременно
        object locker = new object();

        // Флаг контроля проверки количества строк в окне лога
        public bool flag_rows_main = true;

        // Объект класса, взятого из XML с описанием всех модулей (Данный объект содержит структуру всех модулей)
        private PLC _descriptions = new PLC();
        public PLC descriptions
        {
            get { return _descriptions; }
            set { _descriptions = value; }
        }

        // Объект класса работы с базой данных
        WorkWithBD workwithBD = new WorkWithBD();

        // Коллекция, содержащая модули
        private ObservableCollection<Module> _collect = new ObservableCollection<Module>();
        public ObservableCollection<Module> collect
        {
            get {return _collect;}
            set {_collect = value;}
        }

        // Строка, содержащая лог
        public string log
        {
            get;
            set;
        }

        public MainWindow()
        {
            InitializeComponent();
            MW.DataContext = this;

            // Парсинг XML файлов
            ParsXML();
            ParsParamXML();

            // Получение списка ip
            NetworkHelper.GetNets();

            // Заполнение списка именами адаптеров
            for (int i = 0; i < HeaderDriver.list_adapters.Length; i++)
            {
                HeaderDriver._name_adapters.Add(HeaderDriver.list_adapters[i].Name);
            }

            // Проверка списка на наличие необходимого IP
            foreach (IPPair p in HeaderDriver.nets)
            {
                if (p.IP.ToString() == HeaderDriver.ip.ToString())
                    continue;

                cnt++;
            }

            // Сравнение количество вхождений в цикл и количества ip
            if (HeaderDriver.nets.Count == cnt)
            {
                while (true)
                { 
                    // Инициализация окна добавления ip
                    add_IP = new CheckIP();

                    // Проверка результата
                    if ((bool)add_IP.ShowDialog())
                    {
                        // Проверка выбрано ли сетевое устройство
                        if (AddIpAddress() == 0)
                            break;
                        else
                            MessageBox.Show("Выберете сетевое устройство!");
                    }
                    else
                        Application.Current.Shutdown();
                }
            }

            // Преинициализация параметров приложения
            param.ChangeValue();

            // Список Интерфейсов
            HeaderDriver.list_adapters = NetworkInterface.GetAllNetworkInterfaces();

            // Получение списка ip
            NetworkHelper.GetNets();

            // Создание драйвера и привязка к его делегатам методов 
            dr = new Driver();
            dr.add_module_driver += AddModule;
            dr.handler_driver += RecReady;
            dr.data_driver += RecieveData;
            dr.event_lost_connect += LostConnect;
            dr.module_log += WriteModuleLog;
            dr.CheckIPInAdapters();

            // Создаем директорию с файлами конфигурации
            DirectoryInfo dirInfo = new DirectoryInfo(@".\Statistic");
            if (!dirInfo.Exists)
                dirInfo.Create();
        }

        ~MainWindow()
        {
            // Включен ли режим сохранения ip
            if (!HeaderDriver.f_save_ip)
            { 
                // Перебор списка сетевых интерфейсов для нахождения выбранного
                for (int i = 0; i < HeaderDriver.list_adapters.Length; i++)
                {
                    if (HeaderDriver.list_adapters[i].Name == HeaderDriver.select_network_adapter)
                    {
                        // Проверка количества ip
                        if (HeaderDriver.list_adapters[i].GetIPProperties().UnicastAddresses.Count > 1)
                            DeleteTempraryIpAddress();
                        else
                            DinamicIpAddress();
                    }
                }
            }
        }

        /// <summary>
        /// Дабавить ip на выбранный сетевой интерфейс
        /// </summary>
        public int AddIpAddress()
        {
            // Проверка выбран ли сетевой адаптер
            if (HeaderDriver.select_network_adapter == null)
                return 1;

            // Добавление ip
            string command = "netsh interface ip add address \"" + HeaderDriver.select_network_adapter + "\" 10.9.32.1 255.255.255.0"; 

            // Создание новой задачи
            Process cmd = new Process();
            cmd.StartInfo = new ProcessStartInfo(@"cmd.exe");
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.UseShellExecute = false;

            // Запускаем командную строку
            cmd.Start();

            // Вводим команду
            cmd.StandardInput.WriteLine(command);

            // Ожидание применения команды
            Thread.Sleep(5000);

            command = "Exit";
            cmd.StandardInput.WriteLine(command);
            return 0;
        }

        /// <summary>
        /// Удалить добавленный ip
        /// </summary>
        public void DeleteTempraryIpAddress()
        {
            string command = "netsh interface ip delete  address \"" + HeaderDriver.select_network_adapter + "\" 10.9.32.1 255.255.255.0";
            Process cmd = new Process();
            cmd.StartInfo = new ProcessStartInfo(@"cmd.exe");
            cmd.StartInfo.RedirectStandardInput = true;// перенаправить вход
            cmd.StartInfo.UseShellExecute = false;//обязательный параметр, для работы предыдущего
            cmd.Start();//запускаем командную строку
            cmd.StandardInput.WriteLine(command);//вводим команду
        }

        /// <summary>
        /// Перевод из статического в динамический ip
        /// </summary>
        public void DinamicIpAddress()
        {
            string command = "netsh interface ip set  address \"" + HeaderDriver.select_network_adapter + "\" dhcp";
            Process cmd = new Process();
            cmd.StartInfo = new ProcessStartInfo(@"cmd.exe");
            cmd.StartInfo.RedirectStandardInput = true;// перенаправить вход
            cmd.StartInfo.UseShellExecute = false;//обязательный параметр, для работы предыдущего
            cmd.Start();//запускаем командную строку
            cmd.StandardInput.WriteLine(command);//вводим команду
        }

        /// <summary>
        /// Изменение фона таба по наличию связи
        /// </summary>
        /// <param name="ip"> IP и порт модуля </param>
        /// <param name="connected"> Флаг связи с модулем </param>
        public void LostConnect(IPEndPoint ip, bool connected)
        {
            if (collect.Count(x => x.ip.Address.ToString() == ip.Address.ToString()) == 1)
            {
                Module t = collect.Single(x => x.ip.Address.ToString() == ip.Address.ToString());

                // Если связь есть, то устанавливаем белый фон, иначе - красный
                /*if (connected)
                    Dispatcher.Invoke(delegate
                    {
                        t.color = Brushes.White;
                        t.color.Freeze();
                    });
                else
                    Dispatcher.Invoke(delegate
                    {
                        t.color = Brushes.Red;
                        t.color.Freeze();
                    });*/
                if (connected)
                {
                    t.color = Brushes.White;
                    t.color.Freeze();
                }
                else
                {
                    t.color = Brushes.Red;
                    t.color.Freeze();
                }
                    

                OnPropChanged("collect");
            }
        }

        /// <summary>
        /// Обработка буфера и определение типа события
        /// </summary>
        /// <param name="data"> Данные </param>
        /// <param name="ip"> IP и порт модуля </param>
        public void WriteModuleLog(byte[] data, IPEndPoint ip)
        {
            string mask, message;

            // Формирование сообщения о событии 
            message = Encoding.ASCII.GetString(data, 1, data.Length - 1);

            // Определение типа события
            switch (data[0])
            {
                case 1:
                    mask = "Событие";
                    break;
                case 2:
                    mask = "Предупреждение";
                    break;
                case 4:
                    mask = "Ошибка";
                    break;
                case 8:
                    mask = "Дамп приема";
                    break;
                case 16:
                    mask = "Дамп передачи";
                    break;
                case 32:
                    mask = "Трассировка";
                    break;
                case 64:
                    mask = "Пользовательские данные";
                    break;
                case 128:
                    mask = "Сервисные данные";
                    break;
                default:
                    mask = "";
                    break;
            }

            LogWrite("Module", message, mask, ip);
        }

        /// <summary>
        /// Запись лога
        /// </summary>
        /// <param name="device"> Источник события </param>
        /// <param name="message"> Сообщение события </param>
        /// <param name="mask"> Тип события </param>
        /// <param name="ip"> IP и порт модуля </param>
        public void LogWrite(string device, string message, string mask, IPEndPoint ip = null)
        {
            Module m;
            string str;
            byte[] buf = null;
            long elapsedTicks;
            byte[] tempr = new byte[1000];

            // Метка времени, отсчет от 1 января 1970 года
            DateTime start_time = new DateTime(1970, 1, 1);

            // Текущее время
            DateTime current_time = DateTime.Now;

            // Количество тиков в промежуток с 1 января 1970 по настоящее время
            elapsedTicks = current_time.Ticks - start_time.Ticks;

            // Время в промежуток с 1 января 1970 по настоящее время
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

            // Актуальная высота окна с логом
            double ff = gr.ActualHeight;

            // Если IP отсутствует, то источник события приложение и само событие не связано с модулями
            if (ip == null)
            {
                // Формируем строку лога
                str = mask + ":    " + device + "    " + message + "    " + DateTime.Now.ToString() + "." + 
                    (elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString() + "\n";

                // Переформатируем строку и кладем в массив
                tempr = Encoding.ASCII.GetBytes(str);
                buf = new byte[str.Length];
                Array.Copy(tempr, 0, buf, 0, tempr.Length);

                // Добавляем сформированную строку к общей
                this.Dispatcher.Invoke((Action)delegate { log += str; });

                // Производим запись в файл
                using (StreamWriter fStream = new StreamWriter("Log.txt", true))
                {
                    fStream.Write(str);
                    fStream.Close();
                }

                // Проверка высоты окна с логом с помощью длины строки
                if (flag_rows_main)
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        if (Log.ActualHeight > 100)
                        {
                            Log.Height = 115;
                            flag_rows_main = false;
                        }
                    });

                OnPropChanged("log");
            }
            // Если IP присутствует, то источник события - приложение и само событие связано с модулями
            else if (collect.Count(x => x.ip.Address.ToString() == ip.Address.ToString()) == 1)
            {
                m = collect.Single(x => x.ip.Address.ToString() == ip.Address.ToString());

                // Формируем строку лога
                str = mask + ":    " + device + "    " + m.header + "    " + message + "    " + DateTime.Now.ToString() + "." + 
                    (elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString() + "\n";

                // Переформатируем строку и кладем в массив
                tempr = Encoding.ASCII.GetBytes(str);
                buf = new byte[str.Length];
                Array.Copy(tempr, 0, buf, 0, tempr.Length);

                // Добавляем сформированную строку к общей
                this.Dispatcher.Invoke((Action)delegate { log += str; });

                // Производим запись в файл
                using (StreamWriter fStream = new StreamWriter("Log.txt", true))
                {
                    fStream.Write(str);
                    fStream.Close();
                }

                // Проверка высоты окна с логом с помощью длины строки
                if (flag_rows_main)
                    this.Dispatcher.Invoke((Action)delegate
                    {
                        if (Log.ActualHeight > 100)
                        {
                            Log.Height = 115;
                            flag_rows_main = false;
                        }
                    });

                OnPropChanged("log");
            }
        }

        /// <summary>
        /// Парсинг XML файла с описанием структур всех модулей
        /// </summary>
        private void ParsXML()
        {
            string message, mask;

            try
            {
                // Дессириализация файла
                using (Stream fStream = new FileStream("Descriptions.xml", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PLC));
                    descriptions = (PLC)serializer.Deserialize(fStream);
                    Helper.plc = descriptions;
                }

                // Создание события
                mask = "Событие";
                message = "Дессириализация файла с модулями прошла успешно";
                LogWrite("Application", message, mask);
            }
            catch (Exception ex)
            {
                // В случае отсутствия файла или некорректного содержания данных в нем отправляется сообщение об ошибке и приложение закрывается
                MessageBox.Show(ex.ToString());
                Application.Current.MainWindow.Close();
            }
        }

        /// <summary>
        /// Парсинг XML файла с параметрами приложения
        /// </summary>
        private void ParsParamXML()
        {
            string message, mask;

            try
            {
                // Дессириализация файла
                using (Stream fStream = new FileStream("Descriptions_common_parameters.xml", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Collect));
                    common_param = (Collect)serializer.Deserialize(fStream);

                    // Переинициализация параметров
                    ReinitCommonParam();

                    // Создание события
                    mask = "Событие";
                    message = "Дессириализация файла с параметрами прошла успешно";
                    LogWrite("Application", message, mask);
                }
            }
            catch (Exception ex)
            {
                // Создание события
                mask = "Ошибка";
                message = "Дессириализация файла с модулями не пройдена";
                LogWrite("Application", message, mask);

                // В случае отсутствия файла или некорректного содержания данных в нем отправляется сообщение об ошибке и приложение закрывается
                MessageBox.Show(ex.ToString());
                Application.Current.MainWindow.Close();
            }
        }

        /// <summary>
        /// Переинициализация параметров приложения
        /// </summary>
        private void ReinitCommonParam()
        {
            // Перебор всех параметров из XML и добавление их в коллекцию
            foreach (CollectParameter i in common_param.Parameter)
                HeaderDriver._common_parameters.Add(new s_param(i.Name, int.Parse(i.Value), i.Measurement, i.ParameterType));

            // Включение/отключение режима бесконечной отправки данных
            if (int.Parse(common_param.Parameter[9].Value) == 1)
                HeaderDriver.f_unable_infinite = true;
            else
                HeaderDriver.f_unable_infinite = false;
        }

        /// <summary>
        /// Проверка версий на совместимость
        /// </summary>
        /// <param name="xml_vers"> Версия модуля из XML </param>
        /// <param name="ident_vers"> Версия модуля из идентификационного кадра </param>
        /// <returns>
        /// 0 - Версии совпадают
        /// 1 - Версии не совпадают по 4 октету
        /// -1 - Версии не совпадают по 1 октету
        /// -2 - Версии не совпадают по 2 октету 
        /// -3 - Версии не совпадают по 3 октету
        /// </returns>
        private int CompareVers(string xml_vers, string ident_vers)
        {
            int count = 0;
            string message, mask;
            int[] xml = new int[4];
            int[] ident = new int[4];

            // Парсинг строк с версиями 
            for (int i = 0; i <= 6; i++)
            {
                if ((char)xml_vers[i] != '.')
                {
                    xml[count] = (int)xml_vers[i];
                    ident[count] = (int)ident_vers[i];
                    count++;
                }
            }

            // Сравнение октетов версий
            if (xml[0] == ident[0])
                if (xml[1] == ident[1])
                    if (xml[2] >= ident[2])
                        if (xml[3] == ident[3])
                            return 0;
                        else
                        {
                            mask = "Предупреждение";
                            message = "Версия ПО модуля не полностью совместима с версией приложения. Несовместимость по четвертому октету!";
                            return 1;
                        }
                    else
                    {
                        mask = "Ошибка";
                        message = "Версия ПО модуля не совместима с версией приложения. Несовместимость по третьему октету!";
                        LogWrite("Application", message, mask);
                        return -3;
                    }
                else
                {
                    mask = "Ошибка";
                    message = "Версия ПО модуля не совместима с версией приложения. Несовместимость по второму октету!";
                    LogWrite("Application", message, mask);
                    return -2;
                }
            else
            {
                mask = "Ошибка";
                message = "Версия ПО модуля не совместима с версией приложения. Несовместимость по первому октету!";
                LogWrite("Application", message, mask);
                return -1;
            }
        }

        /// <summary>
        /// Поиск и добавление модуля в колекцию, а также проверка нумерации и отправка кадра подтверждения
        /// </summary>
        /// <param name="data"> Принятый буфер с данными </param>
        /// <param name="ip"> IP и порт модуля </param>
        /// <returns></returns>
        int AddModule(byte[] data, IPEndPoint ip)
        {
            string message, mask;

            // Объявление ии инициализация модуля
            Module m = new Module(data, ip, dr);

            // Проверка есть ли модуль в XML файле
            for (int i = 0; i < descriptions.HWModules[0].Module.Length; i++)
            { 
                // Отсечение модулей семейства ТС9ХХ
                if (descriptions.HWModules[0].Module[i].Parameters.Length == 4)
                    continue;

                // Проверка на имя и исполнение
                if (m.pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value == descriptions.HWModules[0].Module[i].Parameters[1].Parameter[1].Value.Text[0] &&
                    m.pr.ident_collect[(byte)SeqIdentDataInCollect.variation].value.Replace(" ", "") == descriptions.HWModules[0].Module[i].Parameters[1].Parameter[5].Value.Text[0])

                    // Проверка на версию
                    if (CompareVers(descriptions.HWModules[0].Module[i].Parameters[1].Parameter[3].Value.Text[0], m.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value) == 0 ||
                        CompareVers(descriptions.HWModules[0].Module[i].Parameters[1].Parameter[3].Value.Text[0], m.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value) == 1)

                        lock (locker)
                        {
                            // Проверка нахождения модуля в колекции
                            if (collect.Count(x => x.ip.Address.ToString() == m.ip.Address.ToString()) == 1)
                            {
                                Module t = collect.Single(x => x.ip.Address.ToString() == m.ip.Address.ToString());

                                // Проверка потока отправки данных на отсутствия
                                if (t.ThreadSendData != null)
                                {
                                    t.ThreadSendData.Abort();
                                    while (t.ThreadSendData.ThreadState != System.Threading.ThreadState.Aborted)
                                        Thread.Sleep(10);
                                    t.ThreadSendData = null;
                                }

                                // Проверка каждого идентификационного параметра на соответствие
                                if ((t.pr.ident_collect[(byte)SeqIdentDataInCollect.status].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.status].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.module_type].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.module_type].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.progname].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.progname].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.variation].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.variation].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.hardvers].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.hardvers].value) &&
                                        (t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value == m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value))
                                {

                                    // Чтение конфигурации из базы данных
                                    workwithBD.WorkBD(t);

                                    // Задание фона таба
                                    LostConnect(t.ip, true);

                                    OnPropChanged("collect");
                                    return 0;
                                }
                                else
                                {
                                    // Перезапись каждого идентификационного параметра
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.status].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.status].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.module_type].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.module_type].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.progname].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.progname].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.variation].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.variation].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.softvers].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.hardvers].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.hardvers].value;
                                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value = m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value;

                                    // Обновление имени таба
                                    t.update_header();

                                    // Чтение конфигурации из базы данных
                                    workwithBD.WorkBD(t);

                                    // Создание события
                                    mask = "Событие";
                                    message = "Модуль в списке обнавлен";
                                    LogWrite("Application", message, mask, m.ip);

                                    // Задание фона таба
                                    LostConnect(t.ip, true);

                                    // Переинициализация параметров приложения
                                    param.ChangeValue();

                                    OnPropChanged("collect");
                                    return 1;
                                }
                            }
                            else
                            {
                                // Заполнение коллекции и привязка к графике
                                m.WriteInCollect(descriptions.HWModules[0].Module[i]);

                                // Чтение конфигурации из базы данных
                                workwithBD.WorkBD(m);

                                // Добавление модуля в коллекцию
                                this.Dispatcher.Invoke((Action)delegate
                                {
                                    collect.Add(m);
                                });

                                // Создание события
                                mask = "Событие";
                                message = "Модуль добавлен в список";
                                LogWrite("Application", message, mask, m.ip);

                                // Переинициализация параметров приложения
                                param.ChangeValue();

                                OnPropChanged("collect");
                                return 1;
                            }
                        }
            }    
            return 0;
        }

        /// <summary>
        /// Перекладка буфера с данными из буфера в парсер
        /// </summary>
        /// <param name="data"> Принятый буфер с данными </param>
        /// <param name="ip"> IP и порт модуля </param>
        public void RecieveData(byte[] data, IPEndPoint ip)
        {
            if (collect.Count(x => x.ip.Address.ToString() == ip.Address.ToString()) == 1)
            {
                Module t = collect.Single(x => x.ip.Address.ToString() == ip.Address.ToString());

                // Проверка на блокировку отправки данных
                if (t.block_send_data)
                    t.block_send_data = false;

                // Прасинг буфера
                t.pr.sign.ParsData(data, t);
            }
        }

        /// <summary>
        /// Метод обработки кадра готовности к работе
        /// </summary>
        /// <param name="data"> Принятый буфер с данными </param>
        /// <param name="ip"> IP и порт модуля </param>
        public void RecReady(byte[] data, IPEndPoint ip)
        {
            if (collect.Count(x => x.ip.Address.ToString() == ip.Address.ToString()) == 1)
            {
                Module t = collect.Single(x => x.ip.Address.ToString() == ip.Address.ToString());

                // Проверка совпадения позиции модуля
                if (t.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value == data[0].ToString())
                {
                    // Проверка применения конфигурации и запуска модуля в работу
                    if (data[1] == (byte)CategoryData.сarOk)
                    {
                        t.ready_work = true;

                        // Создание события
                        LogWrite("Application", "Модуль готов к работе", "Событие", t.ip);
                    }
                    else
                    {
                        // Создание события
                        LogWrite("Application", String.Format("Модуль не готов к работе. Код ошибки: {0}", data[1].ToString()), "Ошибка", t.ip);
                        MessageBox.Show(String.Format("Модуль с ip: {0} не готов к работе. Код ошибки: {1}", ip.Address.ToString(), data[1].ToString()));
                    }

                }
                else
                {
                    // Создание события
                    LogWrite("Application", String.Format("Позиция не совпадает. Позиция в идентификации: {0}, принятая позиция: {1}. ",
                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value, data[1].ToString()), "Ошибка", t.ip);
                    MessageBox.Show(String.Format("Позиция не совпадает. Позиция в идентификации: {0}, принятая позиция: {1}. ",
                    t.pr.ident_collect[(byte)SeqIdentDataInCollect.position].value, data[1].ToString()));
                }

            }
        }

        /// <summary>
        /// Метод отправки конфигурации, срабатывающий при нажатии кнопки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Send_Config(object sender, RoutedEventArgs e)
        {
            int res = 10;
            string mask, message;
            Module m = (Module)Tab.SelectedItem;

            // Проверка выбран ли модуль в окне
            if (m != null)
                // Отправка конфигурации
                res = m.pr.conf.Send_param(m);

            // Результат отправки конфигурации
            if (res == 1)
            {
                mask = "Ошибка";
                message = "Не передан буфер данных";
                LogWrite("Application", message, mask, m.ip);
                MessageBox.Show("Не передан буфер данных");
            }
            else if (res == 2)
            {
                mask = "Ошибка";
                message = "Неверный IP";
                LogWrite("Application", message, mask, m.ip);
                MessageBox.Show("Неверный IP");
            }
            else if (res == 3)
            {
                mask = "Ошибка";
                message = "Не получено подтверждения принятия конфигурации";
                LogWrite("Application", message, mask, m.ip);
                //MessageBox.Show("Не получено подтверждение принятия конфигурации");
            }
            else if (res == 4)
            {
                mask = "Ошибка";
                message = "Отсутствует модуль в списке";
                LogWrite("Application", message, mask, m.ip);
                MessageBox.Show("Отсутствует модуль в списке");
            }
            else if (res == 0)
            {
                mask = "Событие";
                message = "Отправка конфигурации прошла успешно";
                LogWrite("Application", message, mask, m.ip);
            }

            // Отправка команды старт
            dr.SendCommand(true, (byte)ListCommand.Start, m.ip.Address.ToString(), true);
        }

        /// <summary>
        /// Отправка команды старт
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartWork(object sender, RoutedEventArgs e)
        {
            int res;
            string mask, message;
            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                res = dr.SendCommand(true, (byte)ListCommand.Start, m.ip.Address.ToString());
                if (res == 1)
                {
                    mask = "Ошибка";
                    message = "Неверно введен IP";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Неверно введен IP");
                }
                else if (res == 2)
                {
                    mask = "Ошибка";
                    message = "Модуль был не идентифицирован";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль был не идентифицирован");
                }
                else if (res == 3)
                {
                    mask = "Ошибка";
                    message = "Модуль не подтвердил принятия команды 'Старт'";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль не подтвердил принятия команды 'Старт'");
                }
                else if (res == 0)
                {
                    mask = "Событие";
                    message = "Модуль подтвердил принятия команды 'Старт'";
                    LogWrite("Application", message, mask, m.ip);
                }
            }
        }

        /// <summary>
        /// Отправка команды стоп
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopWork(object sender, RoutedEventArgs e)
        {
            int res;
            string mask, message;
            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                res = dr.SendCommand(true, (byte)ListCommand.Stop, m.ip.Address.ToString());
                if (res == 1)
                {
                    mask = "Ошибка";
                    message = "Неверно введен IP";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Неверно введен IP");
                }
                else if (res == 2)
                {
                    mask = "Ошибка";
                    message = "Модуль был не идентифицирован";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль был не идентифицирован");
                }
                else if (res == 3)
                {
                    mask = "Ошибка";
                    message = "Модуль не подтвердил принятия команды 'Стоп'";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль не подтвердил принятия команды 'Стоп'");
                }
                else if (res == 0)
                {
                    mask = "Событие";
                    message = "Модуль подтвердил принятия команды 'Стоп'";
                    LogWrite("Application", message, mask, m.ip);
                }
            }
        }

        /// <summary>
        /// Отправка команды жесткий сброс
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColdReset(object sender, RoutedEventArgs e)
        {
            int res;
            string mask, message;
            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                res = dr.SendCommand(true, (byte)ListCommand.HardReset, m.ip.Address.ToString());
                if (res == 1)
                {
                    mask = "Ошибка";
                    message = "Неверно введен IP";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Неверно введен IP");
                }
                else if (res == 2)
                {
                    mask = "Ошибка";
                    message = "Модуль был не идентифицирован";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль был не идентифицирован");
                }
                else if (res == 3)
                {
                    mask = "Ошибка";
                    message = "Модуль не подтвердил принятия команды 'Жесткий сброс'";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль не подтвердил принятия команды 'Жесткий сброс'");
                }
                else if (res == 0)
                {
                    mask = "Событие";
                    message = "Модуль подтвердил принятия команды 'Жесткий сброс'";
                    LogWrite("Application", message, mask, m.ip);
                }
            }
        }

        /// <summary>
        /// Отправка команды горячего сброса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WarmReset(object sender, RoutedEventArgs e)
        {
            int res;
            string mask, message;
            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                res = dr.SendCommand(true, (byte)ListCommand.WarmReset, m.ip.Address.ToString());
                if (res == 1)
                {
                    mask = "Ошибка";
                    message = "Неверно введен IP";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Неверно введен IP");
                }
                else if (res == 2)
                {
                    mask = "Ошибка";
                    message = "Модуль был не идентифицирован";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль был не идентифицирован");
                }
                else if (res == 3)
                {
                    mask = "Ошибка";
                    message = "Модуль не подтвердил принятия команды 'Горячий сброс'";
                    LogWrite("Application", message, mask, m.ip);
                    MessageBox.Show("Модуль не подтвердил принятия команды 'Горячий сброс'");
                }
                else if (res == 0)
                {
                    mask = "Событие";
                    message = "Модуль подтвердил принятия команды 'Горячий сброс'";
                    LogWrite("Application", message, mask, m.ip);
                }
            }
        }

        /*****************************************************************/
        /*************             SendData             ******************/
        /*****************************************************************/
        private void SendData(object sender, RoutedEventArgs e)
        {
            byte size;
            int num = 0;
            byte[] rs, dt;
            string mask, message;
            byte[] data = new byte[1000];

            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                // Записываем в буфер данные для отправки
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    // Уникальный идентификатор
                    rs = BitConverter.GetBytes(m.pr.in_signals[i].id);
                    Array.Copy(rs, 0, data, num, rs.Length);
                    num += rs.Length;

                    // Маска сигнала
                    data[num++] = (byte)1;

                    // Значение
                    rs = m.pr.in_signals[i].SetValue();

                    if (rs == null)
                        return;

                    size = (byte)rs.Length;
                    data[num++] = size; // Размер значения
                    Array.Copy(rs, 0, data, num, size);
                    num += size;
                }

                // Очищаем поле временного значения сигнала
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    m.pr.in_signals[i].ClearTempr();
                }

                // Конечный id
                rs = BitConverter.GetBytes(0);
                Array.Copy(rs, 0, data, num, rs.Length);
                num += rs.Length;

                // Перекладываем данные в буфер с длиной равной длине данных
                dt = new byte[num];
                Array.Copy(data, 0, dt, 0, num);

                // Закрываем поток отправки данных
                if (m.ThreadSendData != null)
                {
                    m.ThreadSendData.Abort();
                    while (m.ThreadSendData.ThreadState != System.Threading.ThreadState.Aborted)
                        Thread.Sleep(1);
                    m.ThreadSendData = null;
                }

                // Запись в лог события отправки данных
                mask = "Событие";
                message = "Данные отправлены";
                LogWrite("Application", message, mask, m.ip);

                // Создаем поток отправки данных
                m.ThreadSendData = new Thread(delegate () { SendDT(dt, m); });
                m.ThreadSendData.Name = "SendData";
                m.ThreadSendData.Start();
            }
        }

        /*****************************************************************/
        /*************             SendZero             ******************/
        /*****************************************************************/
        private void SendZero(object sender, RoutedEventArgs e)
        {
            byte size;
            int num = 0;
            byte[] rs, dt;
            string mask, message;
            byte[] data = new byte[1000];

            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                // Записываем в буфер данные для отправки
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    // Уникальный идентификатор
                    rs = BitConverter.GetBytes(m.pr.in_signals[i].id);
                    Array.Copy(rs, 0, data, num, rs.Length);
                    num += rs.Length;

                    // Маска сигнала
                    data[num++] = (byte)1;

                    // Значение
                    rs = m.pr.in_signals[i].SetZero();
                    size = (byte)rs.Length;
                    data[num++] = size; // Размер значения
                    Array.Copy(rs, 0, data, num, size);
                    num += size;
                }

                // Очищаем поле временного значения сигнала
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    m.pr.in_signals[i].ClearTempr();
                }

                // Конечный id
                rs = BitConverter.GetBytes(0);
                Array.Copy(rs, 0, data, num, rs.Length);
                num += rs.Length;

                // Перекладываем данные в буфер с длиной равной длине данных
                dt = new byte[num];
                Array.Copy(data, 0, dt, 0, num);

                // Закрываем поток отправки данных
                if (m.ThreadSendData != null)
                {
                    m.ThreadSendData.Abort();
                    while (m.ThreadSendData.ThreadState != System.Threading.ThreadState.Aborted)
                        Thread.Sleep(1);
                    m.ThreadSendData = null;
                }

                // Запись в лог события отправки данных
                mask = "Событие";
                message = "Данные c нулевыми значениями отправлены";
                LogWrite("Application", message, mask, m.ip);

                // Создаем поток отправки данных
                m.ThreadSendData = new Thread(delegate () { SendDT(dt, m); });
                m.ThreadSendData.Name = "SendZero";
                m.ThreadSendData.Start();
            }
        }

        /*****************************************************************/
        /*************             SendOne              ******************/
        /*****************************************************************/
        private void SendOne(object sender, RoutedEventArgs e)
        {
            byte size;
            int num = 0;
            byte[] rs, dt;
            string mask, message;
            byte[] data = new byte[1000];

            Module m = (Module)Tab.SelectedItem;
            if (m != null)
            {
                // Записываем в буфер данные для отправки
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    // Уникальный идентификатор
                    rs = BitConverter.GetBytes(m.pr.in_signals[i].id);
                    Array.Copy(rs, 0, data, num, rs.Length);
                    num += rs.Length;

                    // Маска сигнала
                    data[num++] = (byte)1;

                    // Значение
                    rs = m.pr.in_signals[i].SetOne();
                    size = (byte)rs.Length;
                    data[num++] = size; // Размер значения
                    Array.Copy(rs, 0, data, num, size);
                    num += size;
                }

                // Очищаем поле временного значения сигнала
                for (int i = 0; i < m.pr.in_signals.Count(); i++)
                {
                    m.pr.in_signals[i].ClearTempr();
                }

                // Конечный id
                rs = BitConverter.GetBytes(0);
                Array.Copy(rs, 0, data, num, rs.Length);
                num += rs.Length;

                // Перекладываем данные в буфер с длиной равной длине данных
                dt = new byte[num];
                Array.Copy(data, 0, dt, 0, num);

                // Закрываем поток отправки данных
                if (m.ThreadSendData != null)
                {
                    m.ThreadSendData.Abort();
                    while (m.ThreadSendData.ThreadState != System.Threading.ThreadState.Aborted)
                        Thread.Sleep(1);
                    m.ThreadSendData = null;
                }

                // Запись в лог события отправки данных
                mask = "Событие";
                message = "Данные c единичными значениями отправлены";
                LogWrite("Application", message, mask, m.ip);

                // Создаем поток отправки данных
                m.ThreadSendData = new Thread(delegate () { SendDT(dt, m); });
                m.ThreadSendData.Name = "SendOne";
                m.ThreadSendData.Start();
            }
        }

        /// <summary>
        /// Отправка данных и отображение результата отправки
        /// </summary>
        /// <param name="dt"> Буфер данных </param>
        /// <param name="m"> Экземпляр модуля </param>
        public void SendDT(byte[] dt, Module m)
        {
            // Обработка результата отправки данных
            dr.SendData(dt, m.ip.Address.ToString(), m.ip.Port);
        }

        /*****************************************************************/
        /*************               Start              ******************/
        /*****************************************************************/
        private void CommonStart(object sender, RoutedEventArgs e)
        {
            // Создаем поток и запускаем в нем метод отправки команды Общий Start
            Thread StartThread = new Thread(new ThreadStart(Start));
            StartThread.Name = "Start";
            StartThread.Start();
        }

        /// <summary>
        /// Отправка команды Общий Start
        /// </summary>
        private void Start()
        {
            int res;
            string mask, message;

            // Запись события в лог 
            mask = "Событие";
            message = "Отправлена команда 'Общий старт'";
            LogWrite("Application", message, mask);

            // Отправляем команду Start и обрабатываем рузультат
            res = dr.SendCommand(false, (byte)ListCommand.Start);
            if (res == 1)
            {
                mask = "Ошибка";
                message = "Неверно введен IP";
                LogWrite("Application", message, mask);
                MessageBox.Show("Неверно введен IP");
            }
            else if (res == 2)
            {
                mask = "Ошибка";
                message = "Модуль был не идентифицирован";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль был не идентифицирован");
            }
            else if (res == 3)
            {
                mask = "Ошибка";
                message = "Модуль не подтвердил принятия команды 'Старт'";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль не подтвердил принятия команды 'Старт'");
            }
        }

        /*****************************************************************/
        /*************               Stop              ******************/
        /*****************************************************************/
        private void CommonStop(object sender, RoutedEventArgs e)
        {
            // Создаем поток и запускаем в нем метод отправки команды Общий Stop
            Thread StopThread = new Thread(new ThreadStart(Stop));
            StopThread.Name = "Stop";
            StopThread.Start();
        }

        /// <summary>
        /// Отправка команды Stop
        /// </summary>
        private void Stop()
        {
            int res;
            string mask, message;

            // Запись события в лог 
            mask = "Событие";
            message = "Отправлена команда 'Общий стоп'";
            LogWrite("Application", message, mask);

            // Отправляем команду Stop и обрабатываем рузультат
            res = dr.SendCommand(false, (byte)ListCommand.Stop);
            if (res == 1)
            {
                mask = "Ошибка";
                message = "Неверно введен IP";
                LogWrite("Application", message, mask);
                MessageBox.Show("Неверно введен IP");
            }
            else if (res == 2)
            {
                mask = "Ошибка";
                message = "Модуль был не идентифицирован";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль был не идентифицирован");
            }
            else if (res == 3)
            {
                mask = "Ошибка";
                message = "Модуль не подтвердил принятия команды 'Стоп'";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль не подтвердил принятия команды 'Стоп'");
            }
        }

        /*****************************************************************/
        /*************           WarmReset            ********************/
        /*****************************************************************/
        private void CommonWarmReset(object sender, RoutedEventArgs e)
        {
            // Создаем поток и запускаем нем метод отправки команды Общий WarmReset
            Thread WarmResetThread = new Thread(new ThreadStart(WarmReset));
            WarmResetThread.Name = "WarmReset";
            WarmResetThread.Start();
        }

        /// <summary>
        /// Отправка команды Общий WarmReset
        /// </summary>
        private void WarmReset()
        {
            int res;
            string mask, message;

            // Запись события в лог 
            mask = "Событие";
            message = "Отправлена команда 'Общий горячий сброс'";
            LogWrite("Application", message, mask);

            // Отправляем команду WarmReset и обрабатываем рузультат
            res = dr.SendCommand(false, (byte)ListCommand.WarmReset);
            if (res == 1)
            {
                mask = "Ошибка";
                message = "Неверно введен IP";
                LogWrite("Application", message, mask);
                MessageBox.Show("Неверно введен IP");
            }
            else if (res == 2)
            {
                mask = "Ошибка";
                message = "Модуль был не идентифицирован";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль был не идентифицирован");
            }
            else if (res == 3)
            {
                mask = "Ошибка";
                message = "Модуль не подтвердил принятия команды 'Горячий сброс'";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль не подтвердил принятия команды 'Горячий сброс'");
            }
        }

        /*****************************************************************/
        /*************           ColdReset            ********************/
        /*****************************************************************/
        private void CommonColdReset(object sender, RoutedEventArgs e)
        {
            // Создаем поток и запускаем нем метод отправки команды Общий ColdReset
            Thread HardResetThread = new Thread(new ThreadStart(ColdReset));
            HardResetThread.Name = "HardReset";
            HardResetThread.Start();
        }

        /// <summary>
        /// Отправка команды Общий ColdReset
        /// </summary>
        private void ColdReset()
        {
            int res;
            string mask, message;

            // Запись события в лог 
            mask = "Событие";
            message = "Отправлена команда 'Общий жесткий сброс'";
            LogWrite("Application", message, mask);

            // Отправляем команду ColdReset и обрабатываем рузультат
            res = dr.SendCommand(false, (byte)ListCommand.HardReset);
            if (res == 1)
            {
                mask = "Ошибка";
                message = "Неверно введен IP";
                LogWrite("Application", message, mask);
                MessageBox.Show("Неверно введен IP");
            }
            else if (res == 2)
            {
                mask = "Ошибка";
                message = "Модуль был не идентифицирован";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль был не идентифицирован");
            }
            else if (res == 3)
            {
                mask = "Ошибка";
                message = "Модуль не подтвердил принятия команды 'Жесткий сброс'";
                LogWrite("Application", message, mask);
                MessageBox.Show("Модуль не подтвердил принятия команды 'Жесткий сброс'");
            }
        }

        /// <summary>
        ///  Остановка бесконечной отправки данных модулю
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopInfiniteSend(object sender, RoutedEventArgs e)
        {
            Module m = (Module)Tab.SelectedItem;

            if (m != null)
                dr.StopSend(false, m.ip.Address.ToString());
        }

        /// <summary>
        ///  Остановка бесконечной отправки данных модулю
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommonStopInfiniteSend(object sender, RoutedEventArgs e)
        {
            dr.StopSend(true);
        }

        /// <summary>
        /// Очистка коллекции с модулями
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_collect(object sender, RoutedEventArgs e)
        {
            // Выключение флага поддержки соединения, удаление таймеров и удаление потоков
            foreach (Numeration p in Driver.modules.Values)
            { 
                p.f_close = false;
                p.StopTimers();
                p.DeleteThreeds();
            }

            // Закрытие сокетов
            dr.ResetSocket();

            Thread.Sleep(60);

            // Очищение словоря с кольцевыми буферами
            Helper.ring_buffer.Clear();

            // Очищение словоря с флагами блокировки принятия данных
            Helper.dictionary_enable.Clear();

            lock (Helper.common_lock)
            {
                // Очищение коллекции с сокетами
                Helper.dictionary_threades.Clear();
            }

            // Удаление всех экземпляров модулей из коллекции
            collect.Clear();
        }

        /// <summary>
        /// Метод закрытия приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MW_Closed(object sender, EventArgs e)
        {

            // Выключение флага поддержки соединения
            foreach (Numeration p in Driver.modules.Values)
            { 
                p.f_close = false;
                p.DeleteThreeds();
            }    

            lock (Helper.common_lock)
            {
                Helper.dictionary_threades.Clear();
            }

            // Выключение флага выхода из бесконечных циклов
            if (dr != null)
            { 
                dr.forever = false;

                // Закрытие и зануление сокетов
                if (client != null)
                    //client.Client.Finalize();
                    client.Close();
                client = null;

                // Закрытие драйвера
                if (dr != null)
                    dr.DriverClose();
            }

            // Включен ли режим сохранения ip
            if (!HeaderDriver.f_save_ip)
            {
                // Перебор списка сетевых интерфейсов для нахождения выбранного
                for (int i = 0; i < HeaderDriver.list_adapters.Length; i++)
                {
                    if (HeaderDriver.list_adapters[i].Name == HeaderDriver.select_network_adapter)
                    {
                        // Проверка количества ip
                        if (HeaderDriver.list_adapters[i].GetIPProperties().UnicastAddresses.Count > 1)
                        {
                            DeleteTempraryIpAddress();
                        }
                        else
                            DinamicIpAddress();
                    }
                }
            }

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Вызов сообщения, содержащее описание приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Description_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Данное приложение разработано для поверки модулей ПЛК Elecon");
        }

        /// <summary>
        /// Вызов сообщения, содержащее данные автора
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Author_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Автор: Юшин Олег Евгеньевич\n" +
                "Должность: прграммист отдела разработки программного обеспечения ООО 'ЭлеТим'\n" +
                "Дата начала разработки: 09.08.2021");
        }

        /// <summary>
        /// Вызов сообщения, содержащее версию приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Version_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Версия приложения: 0.0.1.0");
        }

        /// <summary>
        /// Метод закрытия приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        /// <summary>
        /// Вызов диалогового окна общего холодного сброса 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommonColdReset_Click(object sender, RoutedEventArgs e)
        {
            // Инициализация экземпляра окна
            cold_reset = new ColdReset();

            // Проверка результата окна
            if ((bool)cold_reset.ShowDialog())
                ColdReset();
        }

        /// <summary>
        /// Вызов окна с параметрами приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            param = new Params();

            // Проверка результата
            if ((bool)param.ShowDialog())
            {
                int cnt = 0;

                // Заполнение объекта с параметрами приложения данными
                foreach (s_param i in HeaderDriver._common_parameters)
                {
                    common_param.Parameter[cnt].Value = i.val.ToString();
                    cnt++;
                }

                // Проверка значения параметра бесконечной передачи данных
                if (int.Parse(common_param.Parameter[9].Value) == 1)
                    HeaderDriver.f_unable_infinite = true;
                else
                    HeaderDriver.f_unable_infinite = false;

                // Запись значений в XML
                try
                {
                    using (Stream fStream = new FileStream("Descriptions_common_parameters.xml", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Collect));
                        serializer.Serialize(fStream, common_param);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    Application.Current.MainWindow.Close();
                }
            }
            else
            {
                HeaderDriver._common_parameters.Clear();
                ReinitCommonParam();
            }
        }

        /// <summary>
        /// Сохранение значений конфигурационных параметров в базе данных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Config(object sender, RoutedEventArgs e)
        {
            Module m = (Module)Tab.SelectedItem;

            if (workwithBD.WorkSaveBD(m) == 1)
            {
                string mask, message;
                mask = "Ошибка";
                message = "Запись в базу данных невозможна! Не задан заводской номер.";
                LogWrite("Application", message, mask);
                MessageBox.Show("Запись невозможна! Не задан заводской номер.");
            }
            else
            {
                string mask, message;
                mask = "Событие";
                message = "Произведена запись в базу данных";
                LogWrite("Application", message, mask);
            }
        }

        /// <summary>
        /// Метод установки высоты окна лога при изменении ползунка
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GS_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RowDefinitionCollection grid = gr.RowDefinitions;
            Log.Height = grid[4].ActualHeight;
        }

        private void SendZero_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendOne_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
