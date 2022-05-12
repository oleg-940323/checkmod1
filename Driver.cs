using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace checkmod
{
    /// <summary>
    /// Класс, включающий сокет мультикаст и позицию модуля
    /// </summary>
    public class UdpClientAndPos
    {
        public UdpClientAndPos(UdpClient Client, byte pos)
        {
            this.Client = Client;
            this.pos = pos;
        }

        public UdpClient Client;
        public byte pos;
    }

    /// <summary>
    /// Класс Работы с сетевыми интерфейсами
    /// </summary>
    public class NetworkHelper
    {
        // Метод проверки интерфейсов на наличие IP
        public static void GetNets()
        {
            // Добавление в список IP и маски
            try
            {
                // Проверка наличия сетевого интерфейса
                if (HeaderDriver.list_adapters != null)
                {
                    // Очищаем список с IP
                    if (HeaderDriver.nets != null)
                        HeaderDriver.nets.Clear();

                    // Записываем все ip в список со всех интерфейсов
                    foreach (NetworkInterface adapter in HeaderDriver.list_adapters)
                        if (adapter != null)
                        {
                            // Получение объекта с параметрами сетевого интефейса
                            IPInterfaceProperties properties = adapter.GetIPProperties();

                            // Проверка наличия уникаст адреса
                            if (properties != null && properties.UnicastAddresses != null)
                                foreach (UnicastIPAddressInformation uniIPInfo in properties.UnicastAddresses)
                                    // Проверка на наличие ряда параметров интерфейса
                                    if (uniIPInfo != null && uniIPInfo.Address != null && uniIPInfo.IPv4Mask != null && !uniIPInfo.IPv4Mask.Equals(IPAddress.Parse("0.0.0.0")))
                                    {
                                        // Добавление в список IP
                                        if (HeaderDriver.nets == null)
                                            HeaderDriver.nets = new List<IPPair>();

                                        HeaderDriver.nets.Add(new IPPair(uniIPInfo.Address, uniIPInfo.IPv4Mask));
                                    }
                        }
                }
            }
            catch
            {
                //SystemInstances.Engine.MessageService.Error(e.Message);
            }
        }
    }

    /// <summary>
    /// Класс, описывающий объект с IP и маской
    /// </summary>
    public class IPPair
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="ip"> IP </param>
        /// <param name="mask"> Маска </param>
        public IPPair(IPAddress ip, IPAddress mask)
        {
            if (ip == null || mask == null)
                throw new Exception("ip or mask is null");
            this.ip = ip;
            this.mask = mask;
        }

        /// <summary>
        /// IP интерфейса
        /// </summary>
        private IPAddress ip;
        public IPAddress IP
        {
            get { return ip; }
            set { ip = value; }
        }

        /// <summary>
        /// Маска интерфейса
        /// </summary>
        private IPAddress mask;
        public IPAddress Mask
        {
            get { return mask; }
            set { mask = value; }
        }
    }

    /// <summary>
    /// Класс, описывающий модуль на уровне драйвера
    /// </summary>
    public class Numeration
    {
        // Делегат флага выхода из потока обработки цольцевого буфера
        public Flag flag;

        // Позиция модуля
        public byte pos;

        // Количество тиков
        long elapsedTicks;

        // Флаг сообщения о переповторе отправки кадра
        bool f_retry = false;

        // IP модуля
        IPEndPoint ip = null;

        // Поток, в котором запущен метод поддержки связи (Эхо)
        Thread SCThread = null;

        // Флаг управления потоком
        public bool stopf = false;

        // IP костыля
        IPEndPoint ip_multi = null;

        // Флаг выхода из бесконечного цикла в Эхо
        public bool f_close = true;

        // Временный буфер
        byte[] tempr = new byte[10];

        // Объект блокировки доступа к данным
        object locker = new object();

        // Флаг ожидания подтверждения приема кадра шлюза
        public bool WaitRecGT = false;

        // Флаг ожидания подтверждения приема кадра карты сигналов
        public bool WaitRecMap = false;

        // Флаг обозначения первой отправки кадра
        public bool f_first_send = true;

        // Флаг ожидания подтверждения приема конфигурации
        public bool WaitRecConf = false;

        // Флаг ожидания подтверждения приема команды
        public bool WaitRecComm = false;

        // Счетчик отправки эхо
        public int AttemptesSendEcho = 0;

        // Флаг остановки/запуска приема данных от модуля
        public bool stop_send_data = true;

        // Флаг ожидания подтверждения приема выходных данных
        public bool WaitRecOutData = false;

        // Запись в кольцевой буфер
        public bool f_enable_write_data = true;

        // Поток обработки буферов с данными
        public Thread ThreadProccessData = null;

        // Делегат изменения цвета фона таба по наличию/отсутствию связи
        public event EventHandlerLC lost_connect;

        // Сокет client - Общий сокет приложения для сетевого взаимодействия по уникасту
        // Сокет udp_multi - Прием данных от модуля по уникасту
        public UdpClient client = null, udp_multi = null;

        // Запуск таймера периодической отправки Эхо
        public System.Timers.Timer TimerEcho = new System.Timers.Timer(HeaderDriver.period_resend_echo);

        // Запуск таймера ожидания приема кадра от модуля для Эхо
        public System.Timers.Timer TimerWaitEcho = new System.Timers.Timer(HeaderDriver.wait_unable_echo);

        // Запуск таймера переодической передачи пустого кадра
        public System.Timers.Timer TimerPeriodicallySend = new System.Timers.Timer(HeaderDriver.time_resend_empty_buffer);

        // Номер кадра модуля
        public byte num_unicast { get; set; }

        // Номер кадра приложения для отпределенного модуля
        public byte num_app { get; set; }

        // Заголовок таба
        public string header { get; set; }

        public Numeration(IPEndPoint ip, UdpClient client)
        {
            this.client = client;
            this.ip = ip;

            // Получение позиции модуля
            pos = ip.Address.GetAddressBytes()[3];

            TimerPeriodicallySend.Elapsed += OnTimedEventRerSend;
            TimerPeriodicallySend.Start();
        }

        ~Numeration()
        {
            // Существует ли поток Эхо
            if ((SCThread != null) && (SCThread.IsAlive))
            {
                while (true)
                { 
                    try
                    {
                        // Проверка состояния потока
                        if (SCThread.ThreadState == System.Threading.ThreadState.Running)
                            SCThread.Abort();
                        else
                            SCThread. Resume();
                    }
                    catch
                    { }
                    finally
                    {
                        // Ожидание завершения работы потока
                        SCThread.Join();
                    }

                    // Удаление потока
                    if (!SCThread.IsAlive)
                    {
                        SCThread = null;
                        break;
                    }
                }
            }

            // Существует ли поток Обработки кольцевого буфера
            if (ThreadProccessData != null && ThreadProccessData.IsAlive)
            {
                while (true)
                {
                    try
                    {
                        // Проверка состояния потока
                        if (ThreadProccessData.ThreadState == System.Threading.ThreadState.Running)
                            ThreadProccessData.Abort();
                        else
                            ThreadProccessData.Resume();
                    }
                    catch
                    { }
                    finally
                    {
                        // Ожидание завершения работы потока
                        ThreadProccessData.Join();
                    }

                    // Удаление потока
                    if (!ThreadProccessData.IsAlive)
                    {
                        ThreadProccessData = null;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Удаление потоков
        /// </summary>
        public void DeleteThreeds()
        {
            //System.GC.Collect();

            // Изменение флага выхода из потока
            flag();

            while (true)
            {
                try
                {
                    // Существует ли поток Эхо
                    if ((SCThread != null) && (SCThread.IsAlive))
                    {
                        // Проверка состояния потока
                        if (SCThread.ThreadState == System.Threading.ThreadState.Running)
                            SCThread.Abort();
                        else
                            SCThread.Resume();
                    }

                    // Существует ли поток Обработки кольцевого буфера
                    if ((ThreadProccessData != null) && (ThreadProccessData.IsAlive))
                    {

                        // Проверка состояния потока
                        if (ThreadProccessData.ThreadState == System.Threading.ThreadState.Running)
                            ThreadProccessData.Abort();
                        else
                            ThreadProccessData.Resume();
                    }
                }
                catch
                { }
                finally
                {
                    // Ожидание завершения работы потока
                    if (SCThread != null)
                    {
                        SCThread.Join();
                        SCThread = null;
                    }

                    // Ожидание завершения работы потока
                    if (ThreadProccessData != null)
                    {
                        ThreadProccessData.Join();
                        ThreadProccessData = null;
                    }

                }

                if (SCThread == null && ThreadProccessData == null)
                    break;
            }
        }

        /// <summary>
        /// Остановка таймеров модуля
        /// </summary>
        public void StopTimers()
        {
            if (TimerWaitEcho != null)
            {
                TimerWaitEcho.Stop();
                TimerWaitEcho = null;
            }

            if (TimerPeriodicallySend != null)
            {
                TimerPeriodicallySend.Stop();
                TimerPeriodicallySend = null;
            }
        }

        /// <summary>
        /// Событие таймера ожидания получение кадра от модуля
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEventWaitSC(Object source, ElapsedEventArgs e)
        {
            // Проверка наличия запущенного потока для Эхо
            if (SCThread != null)
            {
                TimerEcho. Stop();
                TimerEcho.Start();
            }
            else
            {
                SCThread = new Thread(new ThreadStart(SupportConnect));
                SCThread.Name = "SCThread" + pos.ToString();
                SCThread.Start();
            }

            // Добавление потока в словарь
            lock (Helper.common_lock)
            {
                if (Helper.dictionary_threades.ContainsKey(pos))
                    if (SCThread != null && !Helper.dictionary_threades[pos].Contains(string.Format(SCThread.Name, pos.ToString())))
                        Helper.dictionary_threades[pos].Add(string.Format(SCThread.Name, pos.ToString()));
            }    
                
        }

        /// <summary>
        /// Пддержка связи (Эхо)
        /// </summary>
        public void SupportConnect()
        {
            byte[] tempr;

            // Метка времени, отсчет от 1 января 1970 года
            DateTime start_time = new DateTime(1970, 1, 1);

            // Текущее время
            DateTime current_time = DateTime.Now;

            // Количество тиков в промежуток с 1 января 1970 по настоящее время
            elapsedTicks = current_time.Ticks - start_time.Ticks;

            // Интервал времени в промежуток с 1 января 1970 по настоящее время
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

            // Размер кадра Эхо определен относительно размера типа метки времени (8 байт)
            byte[] data = new byte[17];

            // Значение размера заголовка
            data[(byte)HeaderDriver.HeaderFrame.len] = 8;

            // Тип кадра
            data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fEcho;

            // Действие обратной стороны на кадр (Ответить на данный кадр)
            data[(byte)HeaderDriver.HeaderFrame.reason] = 1;

            // Номер кадра
            data[(byte)HeaderDriver.HeaderFrame.num] = 0;

            // Зарезервивованные байты
            tempr = BitConverter.GetBytes((uint)0);
            Array.Copy(tempr, 0, data, (byte)HeaderDriver.HeaderFrame.res, tempr.Length);

            // Тип метки времени
            data[(byte)HeaderDriver.StartPositionInBuffer] = (byte)HeaderDriver.TimeTypeNum.tTimespec;

            // Дата и время до секунды включительно
            tempr = BitConverter.GetBytes((UInt32)elapsedSpan.TotalSeconds);
            Array.Copy(tempr, 0, data, (byte)HeaderDriver.StartPositionInBuffer + 5, tempr.Length);

            // Количество наносекунд
            tempr = BitConverter.GetBytes((UInt32)(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)));
            Array.Copy(tempr, 0, data, (byte)HeaderDriver.StartPositionInBuffer + 1, tempr.Length);

            // Отправка кадра Эхо
            try
            {
                client.Send(data, data.Length, ip);
            }
            catch
            {
                return;
            }

            // Изменение фона таба
            lost_connect(ip, true);

            // Запуск таймера ожидания ответа на эхо
            AttemptesSendEcho = 0;
            TimerEcho.Start();

            while (f_close)
            { 
                // Проверка количества переотправки кадра Эхо
                if (AttemptesSendEcho >= HeaderDriver.numeration_resend_echo)
                {
                    AttemptesSendEcho = 0;
                    lost_connect(ip, false);
                    break;
                }

                // Проверка флага переотправки кадра
                if (f_retry)
                {
                    // Текущее время
                    current_time = DateTime.Now;

                    // Количество тиков в промежуток с 1 января 1970 по настоящее время
                    elapsedTicks = current_time.Ticks - start_time.Ticks;

                    // Интервал времени в промежуток с 1 января 1970 по настоящее время
                    elapsedSpan = new TimeSpan(elapsedTicks);

                    // Дата и время до секунды включительно
                    tempr = BitConverter.GetBytes((UInt32)elapsedSpan.TotalSeconds);
                    Array.Copy(tempr, 0, data, (byte)HeaderDriver.StartPositionInBuffer + 5, tempr.Length);

                    // Количество наносекунд
                    tempr = BitConverter.GetBytes((UInt32)(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)));
                    Array.Copy(tempr, 0, data, (byte)HeaderDriver.StartPositionInBuffer + 1, tempr.Length);

                    // Отправка кадра
                    try
                    {
                        client.Send(data, data.Length, ip);
                    }
                    catch
                    {
                        return;
                    }

                    f_retry = false;
                }

                // Приостановка потока
                Thread.CurrentThread.Suspend();
            }

            // Удаление из коллекции модуля на уровне драйвера
            Driver.modules.Remove(ip.ToString());

            this.SCThread = null;
        }

        /// <summary>
        /// Событие таймера переповтора отправки кадра Эхо
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEventSC(Object source, ElapsedEventArgs e)
        {
            // Счетчик отправки эхо 
            AttemptesSendEcho++;

            // Включение флага переотпрвки кадра
            f_retry = true;
            
            // Возобновление потока
            if (this.SCThread != null)
                this.SCThread.Resume();
        }

        /// <summary>
        /// Событие таймера переповтора отправки пустого кадра
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEventRerSend(Object source, ElapsedEventArgs e)
        {
            ip_multi = new IPEndPoint(HeaderDriver.ip_multi_rec, HeaderDriver.PortMulti);

            if (udp_multi != null)
                udp_multi.Send(tempr, tempr.Length, ip_multi);

            if (client != null)
                client.Send(tempr, tempr.Length, ip);

            TimerPeriodicallySend.Start();
        }

        /// <summary>
        /// Выход из бесконечного цикла
        /// </summary>
        public void Close()
        {
            f_close = false;
        }
    }

    /// <summary>
    /// Класс драйвера Elebus
    /// </summary>
    public class Driver
    {
        // Сокет приложения
        public UdpClient client;

        // IP и порт приложения
        public IPEndPoint ip_port;

        // Флаг выхода из бесконечных циклов
        public bool forever = true;

        // Флаг выхода из бесконечных циклов
        public bool forever_proccess_data = true;

        // IP и порт модуля
        public IPEndPoint ip = null;

        // Объект блокировки Участка кода
        object locker = new object();

        // Счетчик оправлений конфигурации
        private int AttemptesSend = 0;

        // Флаг переотправки буфера
        private bool RetrySend = false;

        // Флаг сброса данных в кольц5евом буфере
        public bool f_reset_ring_buffer = true;

        // Событие записи лога модуля
        public event EventHandlerReady module_log;

        // Событие отправки полученного буфера на верхний уровень, для парсинга
        public event AddModuleDelegate data_driver;

        // Событие добавления модуля на верхнем уровне
        public event EventHandler add_module_driver;

        // Событие, сообщающая, что модуль готов к работе
        public event EventHandlerReady handler_driver;

        // Событие, сообщающая об состоянии связи с модулем
        public event EventHandlerLC event_lost_connect;

        // Словарь модулей
        static public Dictionary<String, Numeration> modules = new Dictionary<String, Numeration>();

        public Driver()
        {
            // Инициализация таймера максимального времени принятия кадра
            HeaderDriver.TimerRec = new System.Timers.Timer(HeaderDriver.max_wait_time);
            HeaderDriver.TimerRec.Elapsed += OnTimedEvent;
            HeaderDriver.TimerRec.AutoReset = true;

            // Добавление таймеров в словарь 
            if (!HeaderDriver.params_for_driver.ContainsKey("max_wait_time"))
                HeaderDriver.params_for_driver.Add("max_wait_time", HeaderDriver.TimerRec);

            if (!HeaderDriver.params_for_driver.ContainsKey("time_change_value"))
                HeaderDriver.params_for_driver.Add("time_change_value", HeaderDriver.TimerUpdate);
        }

        public void CheckIPInAdapters()
        {
            
            byte[] empty = new byte[10];

            // Инициализация общей конечной сетевой точки
            ip_port = new IPEndPoint(HeaderDriver.ip, HeaderDriver.Port);

            // Инициализация конечной сетевой точки для отправки пустого пакета
            IPEndPoint remote_ip = new IPEndPoint(HeaderDriver.ip_multi_send, HeaderDriver.Port + 1);

            try
            {
                // Инициализация общего сокета
                client = new UdpClient(ip_port);

                // Отправка пустого пакета
                client.Send(empty, empty.Length, remote_ip);
            }
            catch (Exception ex)
            {
                // Удаление потоков
                lock (Helper.common_lock)
                {
                    // Удаление потоков
                    foreach (Numeration pp in Driver.modules.Values)
                        pp.DeleteThreeds();

                    // Очищение словоря с потоками
                    Helper.dictionary_threades.Clear();
                }

                MessageBox.Show(String.Format("IP адрес: {0}\n{1}", HeaderDriver.ip.ToString(), ex.Message));
                Application.Current.MainWindow.Close();
            }

            // Включение асинхронного приема данных
            client.BeginReceive(new AsyncCallback(recv), null);
        }

        /// <summary>
        /// Завершение работы драйвера
        /// </summary>
        public void DriverClose()
        {
            // Выход из беконечных циклов
            forever = false;
            forever_proccess_data = false;

            // Прекращение передачи данных
            foreach (string i in modules.Keys)
                modules[i].stop_send_data = false;

            // Отключение приема данных и удаление сокета
            client.Close();
            client = null;

            GC.Collect(2, GCCollectionMode.Forced);

        }

        /// <summary>
        /// Изменение флага выхода из цикла
        /// </summary>
        public void ChangeFlag()
        {
            forever_proccess_data = false;
        }

        /// <summary>
        /// Сброс нумерации
        /// </summary>
        /// <param name="num"> Модуль </param>
        public void ResetNum(Numeration num)
    {
        // Сбрасываем нумерацию
        num.f_first_send = true;
        num.num_unicast = 0;
        num.num_app = 0;
    }

        /// <summary>
        /// Сброс сокетов для модулей
        /// </summary>
        public void ResetSocket()
        {
            byte[] empty = new byte[10];
            IPEndPoint remote_ip = new IPEndPoint(HeaderDriver.ip_multi_send, HeaderDriver.Port + 1);

            // Отправка пустого буфера
            client.Send(empty, empty.Length, remote_ip);

            // Закрытие сокетов
            lock (locker)
            {
                foreach (Numeration p in modules.Values)
                {
                    p.udp_multi.Dispose();
                    p.udp_multi = null;
                }
            }

            // Очистка словоря с модулями
            modules.Clear();
        }

        /// <summary>
        /// Событие, срабатывающее при получении данных
        /// </summary>
        /// <param name="res"> Объект авсинхронного приема </param>
        private void recv(IAsyncResult res)
        {
            byte[] received = null;

            // Получение буфера данных
            try
            { 
                received = client.EndReceive(res, ref ip);
            }
            catch 
            { 
                return; 
            }

            // Обработка принятого кадра
            AsyncRecieve(received);

            // Запуск ассинхронного приема
            client.BeginReceive(new AsyncCallback(recv), null);
        }

        /// <summary>
        /// Обработчик буфера данных
        /// </summary>
        /// <param name="data"> Буфер данных </param>
        public void AsyncRecieve(byte[] data)
        {
            int i, start = 4;
            byte[] tempr, str;
            Numeration num = null;
            UdpClientAndPos udp_pos = null;
            string modname = null, variation = null;

            try
            {
                // Сброс таймеров Эхо и счетчика переотпраки кадра Эхо
                lock (locker)
                {
                    // Проверка наличия модуля в словаре
                    if (modules.TryGetValue(ip.Address.ToString(), out num))
                    {
                        num.TimerEcho.Stop();
                        num.TimerWaitEcho.Stop();
                        num.AttemptesSendEcho = 0;
                        num.TimerWaitEcho.Start();
                    }
                };

                // Проверка типа кадра
                switch (data[(byte)HeaderDriver.HeaderFrame.type])
                {
                    // Принят идентификационный кадр
                    case (byte)HeaderDriver.type_frame_enum.fident:
                    { 
                        // Проверка на минимальных размер кадра
                        if (data.Length > HeaderDriver.LenHeader)
                            tempr = new byte[data.Length - HeaderDriver.LenHeader];
                        else break;

                        // Убираем из буфера заголовок
                        Array.Copy(data, HeaderDriver.StartPositionInBuffer, tempr, 0, data.Length - HeaderDriver.StartPositionInBuffer);

                        // Получение имени и исполнения модуля
                        for (int j = 0; j <= 2; j++)
                        {
                            // Поиск строк в буфере
                            for (i = start; (char)tempr[i] != '\0'; i++)
                                ;

                            str = new byte[i - start];
                            Array.Copy(tempr, start, str, 0, i - start);

                            if (j == 0)
                                modname = Encoding.ASCII.GetString(str);
                            else if (j == 2)
                                variation = Encoding.ASCII.GetString(str).Trim(new char[] { '\"' });
                            start = ++i;
                        }

                        start = 3;

                        // Добавление модуля в словарь при отсутствии его в словаре
                        if (add_module_driver(tempr, ip) == 1)
                        {
                            // Отпрака подтверждения принятия кадра
                            Ack(data, (byte)HeaderDriver.CodeNum.cOk, ip);

                            lock (locker)
                            {
                                // Проверка нахождения модуля в словаре
                                if (modules.ContainsKey(ip.Address.ToString()))
                                {
                                    // Сброс нумерации модуля
                                    modules.TryGetValue(ip.Address.ToString(), out num);
                                    num.num_app = 0;
                                    num.num_unicast = 0;

                                    // Обновление позиции и заголовка таба модуля
                                    num.pos = tempr[(byte)SeqIdentDataInCollect.position];
                                    num.header = modname + " " + variation + "(" + num.pos.ToString() + ")";
                                }
                                else
                                {
                                    // Инициализация нового модуля
                                    num = new Numeration(ip, client);

                                    num.flag += ChangeFlag;

                                    // Привязка делегата к событию 
                                    num.lost_connect += LC_Event;

                                    // Сброс нумерации
                                    num.num_app = 0;
                                    num.num_unicast = 0;

                                    // Обновление позиции и заголовка таба модуля
                                    num.pos = tempr[(byte)SeqIdentDataInCollect.position];
                                    num.header = modname + " " + variation + "(" + num.pos.ToString() + ")";


                                    if (!HeaderDriver.params_for_driver.ContainsKey("wait_unable_echo" + num.pos.ToString()))
                                        HeaderDriver.params_for_driver.Add("wait_unable_echo" + num.pos.ToString(), num.TimerWaitEcho);

                                    if (!HeaderDriver.params_for_driver.ContainsKey("period_resend_echo" + num.pos.ToString()))
                                        HeaderDriver.params_for_driver.Add("period_resend_echo" + num.pos.ToString(), num.TimerEcho);

                                    // Инициализация таймеров Эхо
                                    num.TimerEcho.Elapsed += num.OnTimedEventSC;
                                    num.TimerEcho.AutoReset = true;

                                    num.TimerWaitEcho.Elapsed += num.OnTimedEventWaitSC;
                                    num.TimerWaitEcho.AutoReset = false;

                                    // Добавление кольцевого буфера модуля в словарь
                                    if (!Helper.ring_buffer.ContainsKey(ip.Address.ToString()))
                                        Helper.ring_buffer.Add(ip.Address.ToString(), new RingBuffer<byte[]>(StartThreads, StopThreads, ip));

                                    lock (Helper.common_lock)
                                    {
                                        // Добавление списка потоков модуля в словарь 
                                        if (Helper.dictionary_threades.ContainsKey(num.pos))
                                            Helper.dictionary_threades.Remove(num.pos);
                                    }

                                    // Добавление флага управления принятия данных модуля в словарь 
                                    if (Helper.dictionary_enable.ContainsKey(num.pos))
                                        Helper.dictionary_enable.Remove(num.pos);

                                    Helper.dictionary_enable.Add(num.pos, true);

                                    // Инициализация конечной точки для прослушивания мультикаст трафика
                                    IPEndPoint ip_port_multi = new IPEndPoint(HeaderDriver.ip, HeaderDriver.PortMulti + num.pos);

                                    // Инициализация конечной точки для отпраки пустого кадра
                                    IPEndPoint ip_port_multi2 = new IPEndPoint(HeaderDriver.ip_multi_rec, HeaderDriver.PortMulti + num.pos);

                                    // Инициализация сокета приема мультикаст трафика
                                    if (num.udp_multi == null)
                                        num.udp_multi = new UdpClient(ip_port_multi);

                                    // Отправка пустого пакета
                                    num.udp_multi.Send(new byte[10], 10, ip_port_multi2);

                                    // Присоединение к муликаст группе
                                    num.udp_multi.JoinMulticastGroup(HeaderDriver.ip_multi_rec);

                                    // Включение бесконечных циклов
                                    forever = true;
                                    forever_proccess_data = true;

                                    udp_pos = new UdpClientAndPos(num.udp_multi, num.pos);

                                    // Запуск ассинхронного приема данных по мультикасту
                                    num.udp_multi.BeginReceive(new AsyncCallback(recvData), udp_pos);

                                    // Запуск потока обработки кольцевого буфера
                                    num.ThreadProccessData = new Thread(delegate () { ProccessData(ip); });
                                    num.ThreadProccessData.Name = "ProccessData " + num.pos.ToString();
                                    num.ThreadProccessData.Start();

                                    lock (Helper.common_lock)
                                    {
                                        // Добавление потока в список модуля, содержащейся в словаре
                                        if (Helper.dictionary_threades.ContainsKey(num.pos))
                                        {
                                            if (!Helper.dictionary_threades[num.pos].Contains(string.Format(num.ThreadProccessData.Name, num.pos.ToString())))
                                                Helper.dictionary_threades[num.pos].Add(string.Format(num.ThreadProccessData.Name, num.pos.ToString()));
                                        }
                                        else
                                            Helper.dictionary_threades.Add(num.pos, new List<string>() { string.Format(num.ThreadProccessData.Name, num.pos.ToString()) });
                                    }

                                    // Добавление модуля в коллекцию
                                    modules.Add(ip.Address.ToString(), num);
                                }
                            }
                        }
                        else
                            // Отпрака подтверждения принятия кадра
                            Ack(data, (byte)HeaderDriver.CodeNum.cDuble, ip);
                        break;
                    }

                    // Принят кадр конфигурации
                    case (byte)HeaderDriver.type_frame_enum.fConfig:
                    { 
                        // Явлется ли данный кадр подтверждением на принятие кадра с конфигурацией
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rAck)
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                modules.TryGetValue(ip.Address.ToString(), out num);
                                if (num.num_app == data[(byte)HeaderDriver.HeaderFrame.num])
                                        num.WaitRecConf = false;
                            }
                        break;
                    }

                    // Принят кадр команды
                    case (byte)HeaderDriver.type_frame_enum.fCommand:
                    { 
                        // Явлется ли данный кадр подтверждением на принятие кадра с командой
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rAck)
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                modules.TryGetValue(ip.Address.ToString(), out num);
                                num.WaitRecComm = false;
                            }
                        break;
                    }

                    // Принят кадр выходных сигналов
                    case (byte)HeaderDriver.type_frame_enum.fDataOutput:
                    { 
                        // Явлется ли данный кадр подтверждением на принятие кадра с выходными сигналами
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rAck)
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                modules.TryGetValue(ip.Address.ToString(), out num);
                                num.WaitRecOutData = false;
                            }
                        break;
                    }

                    // Принят кадр шлюза
                    case (byte)HeaderDriver.type_frame_enum.fGateway:
                    { 
                        // Явлется ли данный кадр подтверждением на принятие кадра шлюза
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rAck)
                        {
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                modules.TryGetValue(ip.Address.ToString(), out num);
                                num.WaitRecGT = false;
                            }
                        }
                        break;
                    }

                    // Принят кадр готовности к работе
                    case (byte)HeaderDriver.type_frame_enum.fReady:
                    { 
                        // Нужно ли отвечать на кадр готовности к работе
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rData_OneAck)
                            // Содержится ли модуль в словаре
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                // Проверка совпадения нумерации для отправки подтверждения на принятие кадра
                                if (modules.TryGetValue(ip.Address.ToString(), out num))
                                    if (num.num_unicast == data[(byte)HeaderDriver.HeaderFrame.num])
                                        Ack(data, (byte)HeaderDriver.CodeNum.cDuble, ip);
                                    else
                                        Ack(data, (byte)HeaderDriver.CodeNum.cOk, ip);

                                // Убираем заголовок из буфера
                                tempr = new byte[data.Length - HeaderDriver.LenHeader];
                                Array.Copy(data, HeaderDriver.LenHeader, tempr, 0, tempr.Length);

                                // Запуск события о готовности работы модуля
                                handler_driver(tempr, ip);
                            }
                        break;
                    }

                    // Принят кадр карты сигналов
                    case (byte)HeaderDriver.type_frame_enum.fMap:
                    {
                        // Явлется ли данный кадр подтверждением на принятие кадра карты сигналов
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rAck)
                        {
                            if (modules.ContainsKey(ip.Address.ToString()))
                            {
                                modules.TryGetValue(ip.Address.ToString(), out num);
                                num.WaitRecMap = false;
                            }
                            break;
                        }
                        break;
                    }

                    // Принят стартовый кадр
                    case (byte)HeaderDriver.type_frame_enum.fStart:
                    { 
                        // Проверка на необходимость подтверждения принятия кадра
                        if (data[(byte)HeaderDriver.HeaderFrame.reason] == (byte)HeaderDriver.ReasonSendEnum.rData_OneAck)
                        {
                            modules.TryGetValue(ip.Address.ToString(), out num);
                            Ack(data, (byte)HeaderDriver.CodeNum.cOk, ip);
                        }
                        break;
                    }

                    // Принят кадр логов
                    case (byte)HeaderDriver.type_frame_enum.fLog:
                    { 
                        // Отправка подтверждения принятия кадра
                        Ack(data, (byte)HeaderDriver.CodeNum.cOk, ip);

                        // Убираем из буфера заголовок
                        tempr = new byte[data.Length - HeaderDriver.LenHeader];
                        Array.Copy(data, HeaderDriver.StartPositionInBuffer, tempr, 0, data.Length - HeaderDriver.StartPositionInBuffer);

                        // Запись события записи лога модуля
                        module_log(tempr, ip);

                        break;
                    }

                    // Принят кадр "Эхо"
                    case (byte)HeaderDriver.type_frame_enum.fEcho:
                    {
                        // Проверка нахождения модуля в коллекции
                        if (modules.TryGetValue(ip.Address.ToString(), out num))
                        {
                            // Проверка кто отправил эхо
                            if (data[(byte)HeaderDriver.HeaderFrame.reason] != 1)
                                Echo(ip, data);
                            else
                            {
                                num.TimerEcho.Stop();
                                num.AttemptesSendEcho = 0;
                            }
                        }
                        break;
                    }

                    default:
                        break;
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Запуск события изменения фона таба
        /// </summary>
        /// <param name="ip"> IP модуля </param>
        /// <param name="state"> Состояние связи </param>
        public void LC_Event(IPEndPoint ip, bool state)
        {
            event_lost_connect(ip, state);
        }

        /// <summary>
        /// Событие, срабатывающее при принятии данных по мультикаст
        /// </summary>
        /// <param name="res"> Объект события </param>
        private void recvData(IAsyncResult res)
        {
            byte pos = 0;
            Numeration num = null;
            byte[] received = null;
            IPEndPoint remote_ip_port = null;

            // Получение сокета через объект события
            UdpClientAndPos udp_pos = (UdpClientAndPos)res.AsyncState;

            try
            {
                // Получение буфера данных
                received = udp_pos.Client.EndReceive(res, ref remote_ip_port);

                // Сброс таймеров Эхо и счетчика переотпраки кадра Эхо
                lock (locker)
                {
                    // Проверка наличия модуля в словаре
                    if (modules.TryGetValue(remote_ip_port.Address.ToString(), out num))
                    {
                        num.TimerEcho.Stop();
                        num.TimerWaitEcho.Stop();
                        num.AttemptesSendEcho = 0;
                        num.TimerWaitEcho.Start();
                    }
                }; 

                // Получение позиции модуля
                pos = udp_pos.pos;

                // Дабавление в кольцевого буфера в словарь
                if (!Helper.ring_buffer.ContainsKey(remote_ip_port.Address.ToString()))
                    Helper.ring_buffer.Add(remote_ip_port.Address.ToString(), new RingBuffer<byte[]>(StartThreads, StopThreads, remote_ip_port));

                // Добавление в словарь управления приемом данных
                if (!Helper.dictionary_enable.ContainsKey(pos))
                    Helper.dictionary_enable.Add(pos, true);

                // Включение приема данных при отсутствии буфера в кольцевом буфуре
                if ((Helper.ring_buffer.ContainsKey(remote_ip_port.Address.ToString())) && (Helper.ring_buffer[remote_ip_port.Address.ToString()].Count == 0))
                    Helper.dictionary_enable[pos] = true;

                // Добавление буфера в кольцевой буфер
                if (Helper.ring_buffer.ContainsKey(remote_ip_port.Address.ToString()) && Helper.dictionary_enable[pos])
                    Helper.ring_buffer[remote_ip_port.Address.ToString()].Enqueue(received);

                // Запуск асинхронного приема данных
                udp_pos.Client.BeginReceive(new AsyncCallback(recvData), udp_pos);
            }
            catch
            {
                if (udp_pos.Client.Client != null)
                    udp_pos.Client.BeginReceive(new AsyncCallback(recvData), udp_pos);
            }
            
        }

        /// <summary>
        /// Метод обработки буфера данных
        /// </summary>
        /// <param name="remote_ip_port"> IP модуля</param>
        public void ProccessData(IPEndPoint remote_ip_port)
        {
            byte[] tempr, data, arr = new byte[10];
            
            
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(remote_ip_port.Address.ToString()), remote_ip_port.Port);

            while (forever_proccess_data)
            {
                // Проверка наличия модуля в кольцевом буфере, а также наличия буфера данных
                if (Helper.ring_buffer.ContainsKey(ip.Address.ToString()) && (Helper.ring_buffer[ip.Address.ToString()].Count > 0))
                {
                    // Получение буфера данных
                    data = Helper.ring_buffer[ip.Address.ToString()].Dequeue();

                    // Отсутствует ли буфер
                    if (data == null)
                        continue;

                    // Проверка на тип кадра
                    if (data[(byte)HeaderDriver.HeaderFrame.type] == (byte)HeaderDriver.type_frame_enum.fModSig)
                    {
                        // Удаление заголовка из буфера и отправка на верхний уровень
                        tempr = new byte[data.Length - HeaderDriver.LenHeader];
                        Array.Copy(data, HeaderDriver.StartPositionInBuffer, tempr, 0, data.Length - HeaderDriver.LenHeader);
                        data_driver(tempr, ip);
                    }

                    // Проверка нахождения модуля в словаре
                    if (modules.ContainsKey(ip.Address.ToString()))
                    {
                        modules.TryGetValue(ip.Address.ToString(), out Numeration num);

                        /*// Пристановка потока
                        if (num.stopf)
                            ProcStopThread(ip);*/
                    }
                }
                else
                    Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Остановка потока
        /// </summary>
        /// <param name="ip"> IP модуля </param>
        public void ProcStopThread(IPEndPoint ip)
        {
            // Проверка нахождения модуля в словаре
            if (modules.ContainsKey(ip.Address.ToString()))
            {
                modules.TryGetValue(ip.Address.ToString(), out Numeration num);

                // Проверка нахождения потока в режиме работы
                if (num.ThreadProccessData.ThreadState == System.Threading.ThreadState.Running)
                {
                    // Приостановка потока
                    num.ThreadProccessData.Suspend();
                    num.stopf = false;
                }
            }
        }

        /// <summary>
        /// Включение флага остановки потока
        /// </summary>
        /// <param name="ip"> IP модуля </param>
        public void StopThreads(IPEndPoint ip)
        {
            // Проверка нахождения модуля в словаре
            if (modules.ContainsKey(ip.Address.ToString()))
            {
                modules.TryGetValue(ip.Address.ToString(), out Numeration num);

                // Включение флага
                num.stopf = true;
            }
        }

        /// <summary>
        /// Введение потока в режим работа
        /// </summary>
        /// <param name="ip"> IP модуля </param>
        public void StartThreads(IPEndPoint ip)
        {
            /*// Проверка нахождения модуля в словаре
            if (modules.ContainsKey(ip.Address.ToString()))
            {
                modules.TryGetValue(ip.Address.ToString(), out Numeration num);

                // Проверка нахождения потока в режиме остановлен
                if (num.ThreadProccessData.ThreadState == System.Threading.ThreadState.Suspended)
                    num.ThreadProccessData.Resume();
            }*/
        }

        /// <summary>
        /// Отправка конфигурации
        /// </summary>
        /// <param name="data"> Буфер с конфигурацией </param>
        /// <param name="ip"> IP модуля </param>
        /// <param name="port"> Порт модуля </param>
        /// <returns></returns>
        public int SendConfig(byte[] data, string ip, int port)
        {
            byte[] tempr;
            IPAddress _ip = null;
            Numeration num = null;

            // Пустой ли буфер
            if (data == null) return 1;

            // Соответствует ли принимаемая строка IP
            if (!IPAddress.TryParse(ip, out _ip))
                return 2;

            tempr = new byte[data.Length + HeaderDriver.LenHeader];

            // Длина заголовка
            tempr[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра
            tempr[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fConfig;

            // Причина передачи
            tempr[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (modules.ContainsKey(ip))
            {
                modules.TryGetValue(ip, out num);

                // Проверяем первый ли это кадр
                if (num.f_first_send)
                {
                    tempr[(byte)HeaderDriver.HeaderFrame.num] = 0;
                    num.f_first_send = false;
                }
                else
                {
                    if (num.num_app == 255)
                        num.num_app = 1;
                    else if (num.num_app >= 0)
                        num.num_app++;

                    tempr[(byte)HeaderDriver.HeaderFrame.num] = num.num_app;
                }
            }
            else
            {
                // Отсутствует модуль в списке
                return 4;
            }

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, tempr, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Данные
            Array.Copy(data, 0, tempr, (byte)HeaderDriver.StartPositionInBuffer, data.Length);

            // Отправка кадра
            client.Send(tempr, tempr.Length, _ip.ToString(), port);

            // Возведение флага ожидания подтверждения принятия кадра модулем
            if (num == null)
            {
                while (!modules.TryGetValue(ip, out num))
                    Thread.Sleep(1);

                num.WaitRecConf = true;
            }

            // Сброс счетчика переотправки кадра
            AttemptesSend = 0;

            // Запуск таймера подтверждения принятия кадра конфигурации
            HeaderDriver.TimerRec.Start();

            while (forever)
            {
                // Проверка включен ли таймер
                if (modules.Count <= 0)
                    break;

                // Выход из цикла при принятии подтверждения
                if (num.WaitRecConf == false)
                {
                    HeaderDriver.TimerRec.Stop();
                    break;
                }

                // Сообщение об отсутствии подтверждения после заданного количества итерации отправки конфигурации
                if (AttemptesSend >= HeaderDriver.numeration_resend)
                {
                    HeaderDriver.TimerRec.Stop();
                    RetrySend = false;
                    MessageBox.Show(string.Format("ip:{0} - не получено подтверждения принятия конфигурации", ip));
                    AttemptesSend = 0;
                    return 3;
                }

                // Переотправка кадра после истечения таймера
                if (RetrySend)
                {
                    client.Send(tempr, tempr.Length, _ip.ToString(), port);
                    RetrySend = false;
                }
            }
            return 0;
        }

        /// <summary>
        /// Отправка данных
        /// </summary>
        /// <param name="data"> Буфер с данными</param>
        /// <param name="ip"> IP модуля </param>
        /// <param name="port"> Порт модуля </param>
        /// <returns></returns>
        public int SendData(byte[] data, string ip, int port)
        {
            Numeration num = null;
            modules[ip].stop_send_data = true;

            byte[] tempr;
            IPAddress _ip;

            // Пустой ли буфер
            if (data == null) return 1;

            // Соответствует ли принимаемая строка IP
            if (!IPAddress.TryParse(ip, out _ip))
                return 2;

            tempr = new byte[data.Length + HeaderDriver.LenHeader];

            // Длина заголовка
            tempr[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра
            tempr[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fDataOutput;

            // Причина передачи
            tempr[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (modules.ContainsKey(ip))
            {
                modules.TryGetValue(ip, out num);

                // Проверяем первый ли это кадр
                if (num.f_first_send)
                {
                    tempr[(byte)HeaderDriver.HeaderFrame.num] = 0;
                    num.f_first_send = false;
                }
                else
                {
                    if (num.num_app == 255)
                        num.num_app = 1;
                    else if (num.num_app >= 0)
                        num.num_app++;

                    tempr[(byte)HeaderDriver.HeaderFrame.num] = num.num_app;
                }
                
            }
            else
                return 4;

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, tempr, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Данные
            Array.Copy(data, 0, tempr, (byte)HeaderDriver.StartPositionInBuffer, data.Length);

            // Отправка кадра
            client.Send(tempr, tempr.Length, _ip.ToString(), port);

            // Возведение флага ожидания подтверждения принятия кадра модулем
            num.WaitRecOutData = true;

            // Сброс счетчика переотправки кадра
            AttemptesSend = 0;

            // Запуск таймера подтверждения принятия кадра конфигурации
            HeaderDriver.TimerRec.Start();

            while (modules[ip].stop_send_data)
            {
                // Проверка включен ли таймер
                if (modules.Count <= 0)
                    break;

                // Выход из цикла при принятии подтверждения
                if (num.WaitRecOutData == false)
                {
                    HeaderDriver.TimerRec.Stop();
                    break;
                }


                // Сообщение об отсутствии подтверждения после заданного количества итерации отправки данных
                if (AttemptesSend >= HeaderDriver.numeration_resend)
                {
                    HeaderDriver.TimerRec.Stop();
                    RetrySend = false;
                    AttemptesSend = 0;
                    return 3;
                }

                // Переотправка кадра после истечения таймера
                if (RetrySend)
                {
                    client.Send(tempr, tempr.Length, _ip.ToString(), port);
                    RetrySend = false;
                }
            }

            // отправляем неограничемое число раз
            if (HeaderDriver.f_unable_infinite)
            {
                // Проверка причины передачи
                tempr[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_NoAck;

                
                // Отправка данных 
                while (modules.ContainsKey(ip) && modules[ip].stop_send_data)
                {
                    client.Send(tempr, tempr.Length, _ip.ToString(), port);
                    Thread.Sleep(HeaderDriver.retry_time);
                }
            }
            return 0;
        }

        /// <summary>
        /// Метод возвращает следующие значения:
        /// *   0 - Конфигурация отправлена и принята модулем в случае уникаста.
        /// *   1 - Неверно введен IP.
        /// *   2 - Модуль был не идентифицирован.
        /// *   3 - Модуль не подтвердил принятия команды.
        /// </summary>
        /// <param name="type_send">Способ отправки. true - уникаст, false - мультикаст.</param>
        /// <param name="command">
        ///  command - Команда. Имеет следущие значения:
        /// *   WarmReset  - Сброс текущего состояния исполнения на модулях и ЦПС (Горячий сброс)
        /// *   ColdReset  - Сброс модуля и переход к процессу идентификации (Жесткий сброс)
        /// *   Stop  - Остановка работы модулей и ЦПС
        /// *   Start - Запуск работы модулей и ЦПС
        /// <param name="ip"> IP не нужно вводить при мультикаст передаче.</param>
        /// </param>
        /// <returns></returns>
        public int SendCommand(bool type_send, byte command, string ip = null, bool once = false)
        {
            int count_start = 0;
            IPAddress _ip = null;
            Numeration num = null;
            byte[] data = new byte[9];

            // Проверка правильности IP
            if (ip != null)
                if (!IPAddress.TryParse(ip, out _ip))
                    return 1;

            // Проверка типа команды по отправки (уникаст, мультикаст)
            if (type_send)
            {
                data = new byte[1 + HeaderDriver.LenHeader];

                // Длина заголовка
                data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

                // Тип кадра
                data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

                // Причина передачи
                data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

                // Нумерация
                if (modules.ContainsKey(ip))
                {
                    modules.TryGetValue(ip, out num);

                    // Проверяем первый ли это кадр
                    if (num.f_first_send)
                    {
                        data[(byte)HeaderDriver.HeaderFrame.num] = 0;
                        num.num_app = 0;
                        num.f_first_send = false;
                    }
                    else
                    {
                        if (num.num_app == 255)
                            num.num_app = 1;
                        else if (num.num_app >= 0)
                            num.num_app++;
                        data[(byte)HeaderDriver.HeaderFrame.num] = num.num_app;
                    }
                }
                else
                    return 2;

                // Резерв
                byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
                Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

                // Команда
                data[(byte)HeaderDriver.StartPositionInBuffer] = command;

                // Отключение постоянной отправки выходных данных
                /*if (command != (byte)HeaderDriver.ListCommand.Start)
                    modules[ip].stop_send_data = false;*/

                // Отправка выходных данных
                client.Send(data, data.Length, _ip.ToString(), HeaderDriver.Port + 1);

                // Отправка команды один раз без подтверждения
                if (once)
                    return 0;

                // Возведение флага ожидания подтверждения
                num.WaitRecComm = true;

                // Сброс счетчика переотправки
                AttemptesSend = 0;

                // Запуск таймера подтверждения принятия кадра
                HeaderDriver.TimerRec.Start();

                while (forever)
                {
                    // Проверка включен ли таймер
                    if (modules.Count <= 0)
                        break;

                    // Проверка принятия кадра модулем
                    if (num.WaitRecComm == false)
                    {
                        HeaderDriver.TimerRec.Stop();

                        // Сброс нумерации при команде жесткого сброса
                        if (command == (byte)HeaderDriver.ListCommand.ColdReset)
                        {
                                num.num_app = 0;
                                num.num_unicast = 0;
                        }

                        break;
                    }

                    // Остановка переотправки при отсутствии ответа на n итерации отправки кадра
                    if (AttemptesSend >= HeaderDriver.numeration_resend)
                    {
                        HeaderDriver.TimerRec.Stop();
                        RetrySend = false;
                        AttemptesSend = 0;
                        return 3;
                    }

                    // Отправка кадра
                    if (RetrySend)
                    {
                        client.Send(data, data.Length, _ip.ToString(), HeaderDriver.Port + 1);
                        RetrySend = false;
                    }
                }

                return 0;
            }
            else
            {

                // Размер заголовка
                data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

                // Тип кадра 
                data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

                // Причина передачи
                data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_Multi;

                // Нумерация
                data[(byte)HeaderDriver.HeaderFrame.num] = 0;

                // Резерв
                byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
                Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

                // Команда
                data[(byte)HeaderDriver.LenHeader] = command;

                // Отключение постоянной отправки выходных данных
               /* if (command != (byte)HeaderDriver.ListCommand.Start)
                    foreach (string i in modules.Keys)
                        modules[i].stop_send_data = false;*/

                // Бесконечная переотправка кадра
                while (count_start < HeaderDriver.num_send_multi)
                {
                    client.Send(data, data.Length, HeaderDriver.ip_multi_send.ToString(), HeaderDriver.Port + 1);
                    count_start++;
                    Thread.Sleep(HeaderDriver.time_multi);
                }

                count_start = 0;
            }

            return 0;
        }

        /// <summary>
        /// Остановка передачи данных
        /// </summary>
        /// <param name="type_send"> Тип передачи (true - общий, false - частный)</param>
        /// <returns></returns>
        public void StopSend(bool type_send, string ip = null)
        {
            // Отключение постоянной отправки выходных данных
            if (!type_send)
                modules[ip].stop_send_data = false;
            else
                foreach (string i in modules.Keys)
                    modules[i].stop_send_data = false;
        }

        /// <summary>
        /// Запуск события при истечении таймера ожидания принятия кадра
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            AttemptesSend++;
            RetrySend = true;
        }

        /// <summary>
        /// Отправка подтверждения принятия кадра
        /// </summary>
        /// <param name="data"> Буфер </param>
        /// <param name="apply"> Код подтверждения </param>
        /// <param name="ip"> IP модуля </param>
        public void Ack(byte[] data, byte apply, IPEndPoint ip)
        {
            Numeration num = null;

            byte[] dt = new byte[HeaderDriver.LenHeader + 1];

            // Размер заголовка
            dt[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра 
            dt[(byte)HeaderDriver.HeaderFrame.type] = data[(byte)HeaderDriver.HeaderFrame.type];

            // Причина передачи
            dt[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rAck;

            // Нумерация
            dt[(byte)HeaderDriver.HeaderFrame.num] = data[(byte)HeaderDriver.HeaderFrame.num];

            // Запись в словарь нумерации модуля
            if (modules.ContainsKey(ip.Address.ToString()))
            {
                modules.TryGetValue(ip.Address.ToString(), out num);
                lock (locker) { num.num_unicast = data[(byte)HeaderDriver.HeaderFrame.num]; };
            }

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, dt, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Код подтверждения
            dt[HeaderDriver.StartPositionInBuffer] = apply;

            // Отправка кадра
             client.Send(dt, dt.Length, ip.Address.ToString(), ip.Port);
        }

        /// <summary>
        /// Отправка подтверждения на кадр Эхо модуля
        /// </summary>
        /// <param name="ip"> IP модуля</param>
        /// <param name="data"> Буфер </param>
        public void Echo(IPEndPoint ip, byte[] data)
        {
            Numeration num = null;

            if (modules.TryGetValue(ip.Address.ToString(), out num))
                // Проверка позиции
                if (num.pos == data[2])
                    // Отправка кадра
                    client.Send(data, data.Length, ip);
        }
    }
}
