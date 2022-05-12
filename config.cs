using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Timers;

namespace checkmod
{
    public class config : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        public config(Driver dr)
        {
            this.dr = dr;
        }

        Driver dr;

        // Метод отправки конфигурации
        public int Send_param(Module m)
        {
            int res;

            byte[] rs;
            // Индекс параметра
            UInt16 count = 1;

            // Начальный номер байта, с которого начинаются данные
            int start_byte = 0;

            byte[] data = new byte[Header.LenFrameConfig];

            // Результат
            data[start_byte++] = (byte)ResultNum.Ok;

            for (int i = 0; i < m.pr.conf_param.Count(); i++)
            {
                rs = null;
                // Индекс параметра
                rs = BitConverter.GetBytes(count++);
                Array.Copy(rs, 0, data, start_byte, rs.Length);
                start_byte += rs.Length;

                rs = m.pr.conf_param[i].GetBytes();
                if (rs == null)
                {
                    start_byte -= 2;
                    continue;
                }

                data[start_byte++] = (byte)rs.Length;  // Размер параметра

                Array.Copy(rs, 0, data, start_byte, rs.Length);
                start_byte += rs.Length;
            }

            // Завершающий индекс
            rs = BitConverter.GetBytes(0);
            Array.Copy(rs, 0, data, start_byte, rs.Length);
            start_byte += rs.Length;

            byte[] arr = new byte[start_byte];
            Array.Copy(data, 0, arr, 0, arr.Length);

            res = dr.SendConfig(arr, m.ip.Address.ToString(), HeaderDriver.Port + 1);

            if (res == 1)
            {
                return 1; // Отсутствуют данные
            }
            else if (res == 2)
            {
                return 2; // Неверный IP
            }
            else if (res == 3)
            {
                return 3; // Не получено подтверждения принятия конфигурации
            }
            else if (res == 4)
            {
                return 4; // Отсутствует модуль в списке
            }
            else
                return 0;
        }
    }
}
