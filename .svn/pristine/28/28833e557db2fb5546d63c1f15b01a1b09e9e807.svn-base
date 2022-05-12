using checkmod.TreeGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace checkmod
{
    /// <summary>
    /// Класс, описывающий модуль
    /// </summary>
    public class Module : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Драйвер программы
        public Driver dr;

        // IP и порт модуля
        public IPEndPoint ip;

        // Нумерация приложения
        public byte num_app = 0;

        // Принятая нумерация уникаст
        public byte num_unicast = 0;

        // Флаг сообщения о сбросе
        public bool flag_reset = true;

        // Принятая нумерация мультикаст
        public byte num_multicast = 0;

        // Флаг ожидания подтверждения приема кадра шлюза
        public bool WaitRecGT = false;

        // Готовность модуля к работе
        public bool ready_work = false;

        // Флаг ожидания подтверждения приема кадра карты сигналов
        public bool WaitRecMap = false;

        // Флаг ожидания подтверждения приема команды
        public bool WaitRecComm = false;

        // Флаг ожидания подтверждения приема конфигурации
        public bool WaitRecConf = false;

        // Стартовый кадр
        public bool start_frame = false;

        // Флаг ожидания подтверждения приема выходных данных
        public bool WaitRecOutData = false;

        // Флаг блокировки принятия данных
        public bool block_send_data = true;

        // Поток отправки данных
        public Thread ThreadSendData = null;

        // Список с логами
        private List<string> _log = new List<string>();
        public List<string> log
        {
            get { return _log; }
            set { _log = value; }
        }

        // Цвет фона таба
        private Brush _color;
        public Brush color
        {
            get
            {
                return _color;
            }

            set
            {
                _color = value;
                OnPropChanged("color");
            }
        }

        // Идентификационные данные
        private pars _pr;
        public pars pr
        {
            get
            {
                return _pr;
            }

            set
            {
                _pr = value;
            }
        }

        // Структура модуля, вычетанная из XML
        private PLCHWModulesModule _mod;
        public PLCHWModulesModule mod { get { return _mod; } set { _mod = value; } }

        // Имя заголовка модуля в окне приложения
        public string header
        {
            get { return pr.full_name; }
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="data"> Буфер с данными класса </param>
        /// <param name="ip"> IP и порт модуля </param>
        /// <param name="dr"> Драйвер программы </param>
        public Module(byte[] data, IPEndPoint ip, Driver dr)
        {
            this.dr = dr;
            _pr = new pars(dr);
            this.ip = ip;
            _pr.check(data);

            // Заполняем коллекцию со статусами
            if (_pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value.StartsWith("TA91"))
            {
                foreach (PLCDataType p in Helper.plc.DataTypes)
                    if (p.name.StartsWith("STATUS_TA91X_T"))
                    {
                        for (byte i = 0; i < 32; i++)
                            if (p.Element[i].description != "Резерв")
                                _pr.sign.SetStatus.Add(new StatusItem(false, p.Element[i].description, i));
                    }
            }
            else if (_pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value.StartsWith("TD9"))
            {
                foreach (PLCDataType pp in Helper.plc.DataTypes)
                    if (pp.name.StartsWith("STATUS_TD9XX_T"))
                    {
                        for (byte i = 0; i < 32; i++)
                            if (pp.Element[i].description != "Резерв")
                                _pr.sign.SetStatus.Add(new StatusItem(false, pp.Element[i].description, i));
                    }
            }
            else if (_pr.ident_collect[(byte)SeqIdentDataInCollect.modname].value.StartsWith("TA902"))
            { 
                foreach (PLCDataType ppp in Helper.plc.DataTypes)
                    if (ppp.name.StartsWith("STATUS_TA902_T"))
                    {
                        for (byte i = 0; i < 32; i++)
                            if (ppp.Element[i].description != "Резерв")
                                _pr.sign.SetStatus.Add(new StatusItem(false, ppp.Element[i].description, i));
                    }
            }
        }   

        /// <summary>
        /// Обновление заголовка модуля
        /// </summary>
        public void update_header()
        {
            OnPropChanged("header");
        }

        /// <summary>
        /// Обновление значений конфигурации в окне
        /// </summary>
        public void UpdateConfCollect()
        {
            OnPropChanged("conf_param");
        }

        /// <summary>
        /// Заполнение коллекции и привязка к графике
        /// </summary>
        /// <param name="item"> Структура модуля, вычетанная из XML </param>
        public void WriteInCollect(PLCHWModulesModule item)
        {
            mod = item;

            // Проверям пустая ли коллекция
            if (pr.conf_param.Count == 0)
            {
                // Заполняем коллекцию с конфигурацией
                if (item.Parameters[2].Parameter != null)
                    for (int i = 0; i < item.Parameters[2].Parameter.Length; i++)
                        pr.conf_param.Add(new TypeParam(item.Parameters[2].Parameter[i]));
            }

            // Заполняем коллекцию с выходными данными
            if (item.Parameters[3].Parameter != null)
                for (int i = 0; i < item.Parameters[3].Parameter.Length; i++)
                    pr.in_signals.Add(new TypeParam(item.Parameters[3].Parameter[i]));

            // Заполняем коллекцию с входными данными
            if (item.Parameters[4].Parameter != null)
                for (int i = 0; i < item.Parameters[4].Parameter.Length; i++)
                    pr.out_signals.Add(new TypeParam(item.Parameters[4].Parameter[i]));

            OnPropChanged("conf_param");
            OnPropChanged("in_signals");
            OnPropChanged("out_signals");
        }
    }
}
