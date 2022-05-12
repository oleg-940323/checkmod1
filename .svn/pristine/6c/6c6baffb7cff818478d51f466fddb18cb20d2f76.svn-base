using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace checkmod
{
    public class Commands : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        ~Commands()
        {
            forever = false;
        }

        private bool forever = true;

        // Кнопка старт
        private int _timecheckcon = 1000; // в мс
        public int timecheckcon
        {
            get { return _timecheckcon; }
            set
            {
                _timecheckcon = value;
                OnPropChanged("timecheccon");
            }
        }

        bool f_retry_send = false;

        // Таймер переповтора отправки выходных данных
        public Timer RetrySendOutData = new Timer(HeaderDriver.retry_time);

        // Запуск таймера ожидания Ack (SendData)
        public Timer SendDataTimer = new Timer(HeaderDriver.max_wait_time);

        // Запуск таймера ожидания Ack (Start)
        public Timer StartTimer = new Timer(HeaderDriver.max_wait_time);

        // Запуск таймера ожидания Ack (Stop)
        public Timer StopTimer = new Timer(HeaderDriver.max_wait_time);

        // Запуск таймера ожидания Ack (WarmReset)
        public Timer WarmResetTimer = new Timer(HeaderDriver.max_wait_time);

        // Запуск таймера ожидания Ack (HardReset)
        public Timer HardResetTimer = new Timer(HeaderDriver.max_wait_time);

        // Флаг переотправки буфера (Start)
        private bool retry_send_start = false;

        // Флаг переотправки буфера (Stop)
        private bool retry_rend_stop = false;

        // Флаг переотправки буфера (WarmReset)
        private bool retry_send_warmreset = false;

        // Флаг переотправки буфера (HardReset)
        private bool retry_send_hardreset = false;

        private void OnTimedEventSendData(Object source, ElapsedEventArgs e)
        {
            // attemptes_send_start++;
            retry_send_start = true;
        }

        private void OnTimedEventRetrySendData(Object source, ElapsedEventArgs e)
        {
            f_retry_send = true;
        }

        /*****************************************************************/
        /*************               Start              ******************/
        /*****************************************************************/
        public void Start(UdpClient client, Module m)
        {
            byte[] data = new byte[(byte)HeaderDriver.LenHeader + 1];

            // Размер заголовка
            data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра 
            data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

            // Причина передачи
            data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (m.num_app == 255)
                m.num_app = 1;
            else if (m.num_app >= 0)
                m.num_app++;

            data[(byte)HeaderDriver.HeaderFrame.num] = m.num_app;

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Команда
            data[(byte)HeaderDriver.LenHeader] = (byte)ListCommand.Start;

            client.Send(data, data.Length, m.ip.Address.ToString(), m.ip.Port);

            // Включаем флаг ожидания приема подтверждения принятия пакета
            m.WaitRecComm = true;
            //attemptes_send_start = 0;

            // Запуск таймера подтверждения принятия кадра команды
            StartTimer.Elapsed += OnTimedEventStart;
            StartTimer.AutoReset = true;
            StartTimer.Start();

            while (forever)
            {
                if (m.WaitRecComm == false)
                {
                    StartTimer.Stop();
                    break;
                }

                /*// Отправка сброса при отсутствии ответа на 3 итерации отправки конфигурации
                if (attemptes_send_start >= 5)
                {
                    StartTimer.Stop();
                    retry_send_start = false;
                    MessageBox.Show(string.Format("ip:{0} - did not Ack on frame of command", m.ip.ToString()));
                    attemptes_send_start = 0;
                    break;
                }*/

                if (retry_send_start)
                {
                    client.Send(data, data.Length, m.ip.Address.ToString(), Header.Port);
                    retry_send_start = false;
                }
            }
        }

        private void OnTimedEventStart(Object source, ElapsedEventArgs e)
        {
            // attemptes_send_start++;
            retry_send_start = true;
        }

        /*****************************************************************/
        /*************               Stop              ******************/
        /*****************************************************************/
        public void Stop(UdpClient client, Module m)
        {

            byte[] data = new byte[(byte)HeaderDriver.LenHeader + 1];

            // Размер заголовка
            data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра 
            data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

            // Причина передачи
            data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (m.num_app == 255)
                m.num_app = 1;
            else if (m.num_app >= 0)
                m.num_app++;

            data[(byte)HeaderDriver.HeaderFrame.num] = m.num_app;

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Команда
            data[(byte)HeaderDriver.LenHeader] = (byte)ListCommand.Stop;

            client.Send(data, data.Length, m.ip.Address.ToString(), m.ip.Port);

            // Включаем флаг ожидания приема подтверждения принятия пакета
            m.WaitRecComm = true;
            //attemptes_send_stop = 0;

            // Запуск таймера подтверждения принятия кадра команды
            WarmResetTimer.Elapsed += OnTimedEventWarmReset;
            WarmResetTimer.AutoReset = true;
            WarmResetTimer.Start();

            while (forever)
            {
                if (m.WaitRecComm == false)
                {
                    WarmResetTimer.Stop();
                    break;
                }

                /*// Отправка сброса при отсутствии ответа на 3 итерации отправки конфигурации
                if (attemptes_send_stop >= 3)
                {
                    WarmResetTimer.Stop();
                    retry_rend_stop = false;
                    MessageBox.Show(string.Format("ip:{0} - did not Ack on frame of command", m.ip.ToString()));
                    attemptes_send_stop = 0;
                    break;
                }*/

                if (retry_rend_stop)
                {
                    client.Send(data, data.Length, m.ip.Address.ToString(), Header.Port);
                    retry_rend_stop = false;
                }
            }
        }

        private void OnTimedEventWarmReset(Object source, ElapsedEventArgs e)
        {
            // attemptes_send_stop++;
            retry_rend_stop = true;
        }

        /*****************************************************************/
        /*************             WarmReset            ******************/
        /*****************************************************************/
        public void WarmReset(UdpClient client, Module m)
        {
            byte[] data = new byte[(byte)HeaderDriver.LenHeader + 1];

            // Размер заголовка
            data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра 
            data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

            // Причина передачи
            data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (m.num_app == 255)
                m.num_app = 1;
            else if (m.num_app >= 0)
                m.num_app++;

            data[(byte)HeaderDriver.HeaderFrame.num] = m.num_app;

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Команда
            data[(byte)HeaderDriver.LenHeader] = (byte)ListCommand.WarmReset;

            client.Send(data, data.Length, m.ip.Address.ToString(), m.ip.Port);

            // Включаем флаг ожидания приема подтверждения принятия пакета
            m.WaitRecComm = true;
            //attemptes_send_warmreset = 0;

            // Запуск таймера подтверждения принятия кадра команды
            StopTimer.Elapsed += OnTimedEventStop;
            StopTimer.AutoReset = true;
            StopTimer.Start();

            while (forever)
            {
                if (m.WaitRecComm == false)
                {
                    StopTimer.Stop();
                    break;
                }

                /*// Отправка сброса при отсутствии ответа на 3 итерации отправки конфигурации
                if (attemptes_send_warmreset >= 3)
                {
                    StopTimer.Stop();
                    retry_send_warmreset = false;
                    MessageBox.Show(string.Format("ip:{0} - did not Ack on frame of command", m.ip.ToString()));
                    attemptes_send_warmreset = 0;
                    break;
                }*/

                if (retry_send_warmreset)
                {
                    client.Send(data, data.Length, m.ip.Address.ToString(), Header.Port);
                    retry_send_warmreset = false;
                }
            }
        }

        private void OnTimedEventStop(Object source, ElapsedEventArgs e)
        {
            // attemptes_send_warmreset++;
            retry_send_warmreset = true;
        }

        /*****************************************************************/
        /*************             HardReset            ******************/
        /*****************************************************************/
        public void HardReset(UdpClient client, Module m)
        {
            byte[] data = new byte[(byte)HeaderDriver.LenHeader + 1];

            // Размер заголовка
            data[(byte)HeaderDriver.HeaderFrame.len] = (byte)HeaderDriver.LenHeader;

            // Тип кадра 
            data[(byte)HeaderDriver.HeaderFrame.type] = (byte)HeaderDriver.type_frame_enum.fCommand;

            // Причина передачи
            data[(byte)HeaderDriver.HeaderFrame.reason] = (byte)HeaderDriver.ReasonSendEnum.rData_OneAck;

            // Нумерация
            if (m.num_app == 255)
                m.num_app = 1;
            else if (m.num_app >= 0)
                m.num_app++;

            data[(byte)HeaderDriver.HeaderFrame.num] = m.num_app;

            // Резерв
            byte[] rs = BitConverter.GetBytes(HeaderDriver.reserve);
            Array.Copy(rs, 0, data, (byte)HeaderDriver.HeaderFrame.res, rs.Length);

            // Команда
            data[(byte)HeaderDriver.LenHeader] = (byte)ListCommand.ColdReset;

            client.Send(data, data.Length, m.ip.Address.ToString(), m.ip.Port);

            // Включаем флаг ожидания приема подтверждения принятия пакета
            m.WaitRecComm = true;
            //attemptes_send_hardreset = 0;

            // Запуск таймера подтверждения принятия кадра команды
            HardResetTimer.Elapsed += OnTimedEventHardReset;
            HardResetTimer.AutoReset = true;
            HardResetTimer.Start();

            while (forever)
            {
                if (m.WaitRecComm == false)
                {
                    HardResetTimer.Stop();
                    break;
                }

                /*// Отправка сброса при отсутствии ответа на 3 итерации отправки конфигурации
                if (attemptes_send_hardreset >= 3)
                {
                    HardResetTimer.Stop();
                    retry_send_hardreset = false;
                    MessageBox.Show(string.Format("ip:{0} - did not Ack on frame of command", m.ip.ToString()));
                    attemptes_send_hardreset = 0;
                    break;
                }*/

                if (retry_send_hardreset)
                {
                    client.Send(data, data.Length, m.ip.Address.ToString(), Header.Port);
                    retry_send_hardreset = false;
                }
            }
        }

        private void OnTimedEventHardReset(Object source, ElapsedEventArgs e)
        {
            // attemptes_send_hardreset++;
            retry_send_hardreset = true;
        }
    }
}
