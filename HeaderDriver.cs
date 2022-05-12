using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    
    class HeaderDriver
    {
        // IP адрес для мультикаст передачи
        public static IPAddress ip_multi_send = IPAddress.Parse("239.255.255.251");

        // IP адрес для мультикаст приема
        public static IPAddress ip_multi_rec = IPAddress.Parse("239.255.255.252");

        // IP адрес для создания сокета
        public static IPAddress ip =IPAddress.Parse("10.9.32.1");

        // Максимальное время принятия кадра
        public static int max_wait_time = 10;

        // Максимальное время принятия кадра
        public static bool _f_save_ip = false;
        public static bool f_save_ip
        {
            get => _f_save_ip;
            set { _f_save_ip = value; }
        }

        // Выбранный IP
        public static IPAddress select_ip = null;

        // Выбранный сетевой интерфейс
        public static string select_network_adapter = null;

        // Выбранная маска
        public static IPAddress select_mask = null;

        // Список IP с масками
        static public List<IPPair> nets = null;

        // Список Интерфейсов
        static public NetworkInterface[] list_adapters = NetworkInterface.GetAllNetworkInterfaces();

        // Список имен интерфейсов
        static public List<string> _name_adapters = new List<string>();
        static public List<string> name_adapters
        {
            get => _name_adapters;
        }

        // Количество переотправки кадра
        public static int numeration_resend = 5;

        // Время переотправки кадра с выходными данными
        public static int retry_time = 30;

        // Время переотправки кадра с пустым буфером (не является параметром приложения)   
        public static int time_resend_empty_buffer = 500;

        // Время изменения значения в окне
        public static int change_time = 1000;

        // Время паузы между переотправкой кадров
        public static int time_multi = 1;

        // Количество переповторов отправки кадров по мультикаст
        public static int num_send_multi = 5;

        // Порт Приложения
        public static int Port = 43021;

        // Порт Приложения (Мультикаст)
        public static int PortMulti = 43100;

        // Значение резерва в заголовке
        public static int reserve = 0;

        // Длина заголовка кадра
        public static int LenHeader = 8;

        // Длина кадра конфигурации
        public static int LenFrameConfig = 100;

        // Время ожидания включения эхо после последнего полученного кадра от модуля
        public static int wait_unable_echo { get; set; } = 50;

        // Период переотправки кадра эхо
        public static int period_resend_echo = 30;

        // Словарь параметров
        public static Dictionary<string, System.Timers.Timer> params_for_driver = new Dictionary<string, System.Timers.Timer>();

        // Таймер обновления графики
        public static System.Timers.Timer TimerUpdate = new System.Timers.Timer(change_time);

        // Запуск таймера максимального времени принятия кадра
        public static System.Timers.Timer TimerRec = new System.Timers.Timer(max_wait_time);

        // Номер байта с которого начинаются строки в идентификационном кадре
        public static int StartPosInIdentFrame = 4;

        // Номер байта в буфере с которого начинаются данные
        public static int StartPositionInBuffer = 8;

        // Количество переотправки кадра эхо
        public static int numeration_resend_echo = 5;

        // Флаг включения бесконечной переотправки кадра
        public static bool f_unable_infinite = false;

        // Коллекция параметров приложения
        static public ObservableCollection<s_param> _common_parameters = new ObservableCollection<s_param>();

        // Типы кадров
        public enum type_frame_enum : byte
        {
            fident = 1,          /* Идентификационный кадр */
            fConfig = 2,         /* Кадр конфигурации */
            fCommand = 3,        /* Кадр с командой */
            fModSig = 4,         /* Кадр сигналов модуля*/
            fDataOutput = 5,     /* Кадр с выходными данными */
            fGateway = 6,        /* Кадр для работы со шлюзом */
            fReady = 7,          /* Кадр готовности модуля к работе */
            fMap = 8,            /* Кадр с картой ПЛК */
            fStart = 9,          /* Кадр старта */
            fLog = 10,           /* Кадр логов */
            fConfFile = 11,      /* Кадр файла конфигурации */
            fEcho = 200          /* Эхо кадр */
        }

        // Причина передачи
        public enum ReasonSendEnum : byte
        {
            rAck = 0,               /* Подтверждение */
            rData_Multi = 1,        /* Передача по адресу мультикаст */
            rData_NoAck = 2,        /* Передача без подтверждения */
            rData_OneAck = 3,       /* Передача с одним подтверждением */
        }

        // Поле кода подтверждения
        public enum CodeNum : byte
        {
            cOk = 0,                /* Кадр успешно принят */
            cDuble = 1,             /* Принят кадр-дубль (номер кадра совпадает с номером предыдущего кадра) */
            cBusy = 2,              /* Кадр не может быть принят в данный момент (процесс занят) */
            сNotReady = 3,          /* Код ошибки. Отсутствует конфигурация, кадр не может быть обработан */
            cNoOutput = 4,          /* Код ошибки. У модуля отсутствуют выходные сигналы */
            cNotSupported = 5,      /* Код ошибки. Функция не поддерживается */
            cTrxFailed = 6,         /* Код ошибки. Ошибка при передаче данных */
            cNotStarted = 7,        /* Код ошибки. Модуль не получал команды на запуск */
            cError = 50             /* Код ошибки. Начало диапазона уникальных ошибок устройства (50-255) */
        }

        // Типы меток времени
        public enum TimeTypeNum : byte
        {
            /* tNone = 0,          *//* Метка времени отсутствует *//*

             tCP24 = 1,          *//* Метка времени выдается в формате CP24Время2a, 3 байта *//*

             tTimeVal = 2,       *//* Метка времени выдается в формате Timeval (8 байт): 4 младших байта – время относительно начала 1970 года в секундах,
                                 4 старших байта – значение в микросекундах относительно начала секунд *//*

             tCP32 = 3,          *//* Метка времени выдается в формате CP32Время2a (4 байта): время в 10-ти миллисекундных тиках относительно начала года *//*

             tGPS = 4,           *//* Метка времени выдается в формате GPS (8 байт): 6 байт – метка GPS 
                                 (4 младших байта – секунды относительно начала недели + 2 байта – количество недель относительно 6 января 1980 года)
                                 и 2 байта – значение в миллисекундах относительно начала секунд *//*

             tCP56 = 5,          *//* Метка времени выдается в формате CP56Время2a 7 байт *//*

             tNMEA = 6,          *//* Метка времени выдается в формате NMEA 8 байт *//*

             tmsTimeVal = 7,     *//* Метка времени выдается в формате Timeval+msek 10 байт */

            tTimespec = 8       /* Метка времени выдается в формате Timespec 8 байт; 4 младших байта – время относительно начала 1970 года в секундах,
                            4 старших байта – значение в наносекундах относительно начала секунд */
        }

        // Номер байта в заголовке кадра
        public enum HeaderFrame : byte
        {
            len = 0,
            type = 1,
            reason = 2,
            num = 3,
            res = 4
        }

        // Список команд
        public enum ListCommand : byte
        {
            WarmReset = 1,      /* Сброс текущего состояния исполнения на модулях и ЦПС */
            ColdReset = 2,      /* Сброс модуля и переход к процессу идентификации */
            Stop = 3,           /* Остановка работы модулей и ЦПС */
            Start = 4           /* Запуск работы модулей и ЦПС */
        }
    }
}
