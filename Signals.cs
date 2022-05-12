﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace checkmod
{

    public class StatusItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        public StatusItem(bool value, string discription, byte id)
        {
            this.value = value;
            this.discription = discription;
            this.id = id;
        }

        private bool _value;
        public bool value { get => _value; 
            set { _value = value; OnPropChanged("value"); } }

        public string discription { get; set; }

        public byte id { get; set; }
    }

    public class Signals : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        Driver dr;

        object locker = new object();

        // Дискриптор файла бинар
        BinaryWriter writer = null;

        // Дискриптор файла кфг
        StreamWriter fStream = null;

        // Дискриптор файла для TD9
        StreamWriter file_for_TD9 = null;

        private bool forever = true;

        // Флаг блокировки обновления выходных данных
        public bool f_lock_update_data = true;

        UInt32 id, seq = 1;

        // Длина выходных данных
        public int len_out_data;

        string path = null, path_cfg = null, cfg = null;

        public bool write_sign { get; set; } = false;

        DateTime current_time;

        List<byte[][]> values = null;

        TimeSpan elapsedSpan;

        // Массив битов
        UInt32[] bits = new UInt32[32] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384,
            32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864,
            134217728, 268435456, 536870912, 1073741824, 2147483648};

        // Статус
        private UInt32 _stat;
        public UInt32 stat
        {
            get { return _stat; }
            set
            {
                _stat = value;
                for (int k = 0; k < 31; k++)
                {
                    if (SetStatus.Count(x => x.id == k) > 0)
                    {
                        SetStatus.Single(x => x.id == k).value = (_stat & bits[k]) > 0;
                    }
                }
                OnPropChanged("stat");
            }
        }

        // Коллекция статусов
        private ObservableCollection<StatusItem> _SetStatus = new ObservableCollection<StatusItem>();
        public ObservableCollection<StatusItem> SetStatus
        {
            get => _SetStatus;

            set { _SetStatus = value; OnPropChanged("SetStatus"); }
        }

        public Signals(Driver dr)
        {
            this.dr = dr;
        }

        ~Signals()
        {
            forever = false;
        }

        // Метод парсинга буфера данных
        public void ParsData(byte[] data, Module m)
        {
           // Номер байта 
            int num;

            byte size = 0, volt;
            byte[] tempr2 = null;
            long elapsedTicks;
            string[] val = new string[12], str = null;
            values = new List<byte[][]>();

            // Текущее время
            current_time = DateTime.Now;

            // Количество тиков в промежуток с 1 января 1970 по настоящее время
            elapsedTicks = current_time.Ticks - Helper.start_time.Ticks;

            elapsedSpan = new TimeSpan(elapsedTicks);

            int i = 0, j = 0, ind = 0, qq;
            num = 0;
            byte[] tempr = new byte[5000], arr = null;



            // Статус модуля
            if (stat != BitConverter.ToUInt32(data, num))
                stat = BitConverter.ToUInt32(data, num);

            num += 4;
            try
            {
                if (write_sign)
                {
                    lock (locker)
                    {
                        if (dr.f_reset_ring_buffer)
                            dr.f_reset_ring_buffer = false;
                    }

                    if (writer == null && !m.header.StartsWith("TD9"))
                    {
                        path = @".\Statistic\" + m.header + ".dat";
                        path_cfg = @".\Statistic\" + m.header + ".cfg";
                        writer = new BinaryWriter(File.Open(path, FileMode.Append));

                        if (fStream != null)
                            fStream.Close();
                        fStream = new StreamWriter(File.Open(path_cfg, FileMode.Create));
                    }
                    else if (file_for_TD9 == null && m.header.StartsWith("TD9"))
                    {
                        path = @".\Statistic\" + m.header + ".txt";
                        file_for_TD9 = new StreamWriter(File.Open(path, FileMode.Append));
                        if (file_for_TD9.BaseStream.Length == 0)
                            cfg = "ID сигнала\tЗначение\tВремя\n";
                    }

                    // Запись в конфигурационный файл
                    if (cfg == null && !m.header.StartsWith("TD9"))
                    {
                        if (m.header.StartsWith("TA902"))
                        {
                            cfg = $"{m.header},{m.header},2013\n{m.mod.Parameters[4].Parameter.Length - 1}," +
                                $"{m.mod.Parameters[4].Parameter.Length - 1}A,0D\n";

                            str = m.header.Split('=', '(', ',');

                            volt = byte.Parse(str[3]);

                            for (int q = 1; q <= m.mod.Parameters[4].Parameter.Length - 1; q++)
                            {
                                cfg += $"{q},AInput{q},,,";

                                if (q - 1 < volt / 2)
                                {
                                    cfg += "V,1.0,1.0,0,0,10,1.0,1.0,S\n";
                                }
                                else
                                {
                                    cfg += "mA,1.0,1.0,0,0,40,1.0,1.0,S\n";
                                }
                            }

                            cfg += $"50\n1\n4000,400000\n{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\n" +
                                $"{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\nFLOAT32\n1";
                        }
                        else if (m.header.StartsWith("TA901"))
                        {
                            cfg = $"{m.header},{m.header},2013\n{m.mod.Parameters[3].Parameter.Length},{m.mod.Parameters[3].Parameter.Length}A,0D\n";

                            str = m.header.Split('=', '(', ',');

                            volt = byte.Parse(str[3]);

                            for (int q = 1; q <= m.mod.Parameters[3].Parameter.Length; q++)
                            {
                                cfg += $"{q},DInput{q},,,";

                                if (q - 1 < volt)
                                {
                                    cfg += "V";

                                    if ((string.IsNullOrEmpty(m.mod.Parameters[3].Parameter[q - 1].Min)) ||
                                    (string.IsNullOrEmpty(m.mod.Parameters[3].Parameter[q - 1].Max)))
                                    {
                                        cfg += $",0.1,0.0,0,{Helper.MinMaxOfType(m.mod.Parameters[3].Parameter[q - 1].datatype).min}," +
                                          $"{Helper.MinMaxOfType(m.mod.Parameters[3].Parameter[q - 1].datatype).max},220.00,0.10,S\n";
                                    }
                                    else
                                        cfg += $",0.1,0.0,0,{m.mod.Parameters[3].Parameter[q - 1].Min},{m.mod.Parameters[3].Parameter[q - 1].Max},220.00,0.10,S\n";
                                }
                                else
                                {
                                    cfg += "A";

                                    if ((string.IsNullOrEmpty(m.mod.Parameters[3].Parameter[q - 1].Min)) ||
                                    (string.IsNullOrEmpty(m.mod.Parameters[3].Parameter[q - 1].Max)))
                                    {
                                        cfg += $",0.1,0.0,0,{Helper.MinMaxOfType(m.mod.Parameters[3].Parameter[q - 1].datatype).min}," +
                                          $"{Helper.MinMaxOfType(m.mod.Parameters[3].Parameter[q - 1].datatype).max},1000.00,5.00,S\n";
                                    }
                                    else
                                        cfg += $",0.1,0.0,0,{m.mod.Parameters[3].Parameter[q - 1].Min},{m.mod.Parameters[3].Parameter[q - 1].Max},1000.00,5.00,S\n";
                                }

                            }
                            cfg += $"50\n1\n1000000,199999\n{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\n" +
                                $"{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\nFLOAT32\n1";
                        }
                        else if (m.header.StartsWith("TA9"))
                        {
                            cfg = $"{m.header},{m.header},2013\n{(m.mod.Parameters[3].Parameter.Length * 2) - 5}," +
                                $"{(m.mod.Parameters[3].Parameter.Length * 2) - 5}A,0D\n";

                            str = m.header.Split('=', '(', ',');

                            volt = byte.Parse(str[3]);
                            volt += byte.Parse(str[3]);

                            for (int q = 1; q <= (m.mod.Parameters[3].Parameter.Length * 2) - 5;)
                            {
                                for (qq = q; qq < q + 2; qq++)
                                {
                                    if (qq == 26)
                                        break;

                                    if (qq % 2 == 1)
                                        cfg += $"{qq},VAL_SIG{qq},,,";
                                    else
                                        cfg += $"{qq},RMS_SIG{qq},,,";

                                    if (qq - 1 < volt)
                                    {
                                        cfg += "V,1.0,0.0,0,-1000,1000,1.0,1.0,S\n";
                                    }
                                    else
                                    {
                                        cfg += "A,1.0,0.0,0,-1000,1000,1.0,1.0,S\n";
                                    }
                                }
                                q = qq;
                            }
                            cfg += $"50\n1\n4000,400000\n{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\n" +
                                $"{current_time.Day.ToString()}/{current_time.Month.ToString()}/{current_time.Year.ToString()}," +
                                $"{current_time.Hour.ToString()}:{current_time.Minute.ToString()}:{current_time.Second.ToString()}." +
                                $"{(elapsedTicks * 100 - (long)elapsedSpan.TotalSeconds * (long)Math.Pow(10, 9)).ToString()}\nFLOAT32\n1";
                        }
                    }


                    while (forever)
                    {
                        // Идентификационный номер сигнала
                        id = BitConverter.ToUInt32(data, num);
                        num += 4;
                        if (id == 0 || num >= data.Length) break;

                        // Входа для ЦП или выхода для модуля
                        if (id > 1000000)
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id >= 1000016)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                                else if (id == 1000014)
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.mod.Parameters[3].Parameter[13].datatype);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);

                                    m.pr.out_signals[j].ReInit(tempr2);

                                    values.Add(Helper.GetInstVal(tempr2));

                                    num += size;
                                }
                                else if (id == 1000015)
                                {
                                    j++;

                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                                else
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                            }
                            else
                            {
                                if (id - 1000000 > m.pr.out_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }

                                num++;
                                m.pr.out_signals[j].id = id;

                                size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                tempr2 = new byte[size];
                                Array.Copy(data, num, tempr2, 0, size);
                                m.pr.out_signals[j].ReInit(tempr2);

                                j++;

                                num += size;
                            }
                        }
                        // Выхода для ЦП или входа для модуля
                        else
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id > 1000015)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }
                            else
                            {
                                if (id > m.pr.in_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }

                            num++;
                            m.pr.in_signals[i].id = id;

                            size = Helper.SizeOfType(m.pr.in_signals[i].data_type);
                            tempr2 = new byte[size];
                            Array.Copy(data, num, tempr2, 0, size);
                            m.pr.in_signals[i].ReInit(tempr2);
                            i++;
                            num += size;
                        }
                    }


                    if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                    {
                        foreach (byte[][] p in values)
                        {
                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq).Length;

                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq++).Length;

                            for (int n = 0; n < 12; n++)
                            {
                                Array.Copy(p[n], 0, tempr, ind, 4);
                                ind += 4;

                                Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                                ind += 4;
                            }

                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[12].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;

                            arr = new byte[ind];
                            Array.Copy(tempr, 0, arr, 0, ind);

                            writer.Write(arr);

                            ind = 0;
                        }
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TD9"))
                    {
                        for (int ii = 0; ii < m.pr.in_signals.Count; ii++)
                        {
                            if (m.pr.in_signals[ii].children[2].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.in_signals[ii].id.ToString() + "\t" + m.pr.in_signals[ii].children[2].value +
                                    "\t" + m.pr.in_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].children[2].f_has_changed = false;
                                }
                            }

                            if (m.pr.out_signals[ii].children[2].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.out_signals[ii].id.ToString() + "\t" + m.pr.out_signals[ii].children[2].value +
                                    "\t" + m.pr.out_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].children[2].f_has_changed = false;
                                }
                            }
                        }
                        file_for_TD9.Write(cfg);
                        cfg = null;
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA902"))
                    {
                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq).Length;

                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq++).Length;

                        for (int n = 0; n < 6; n++)
                        {
                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;
                        }

                        arr = new byte[ind];
                        Array.Copy(tempr, 0, arr, 0, ind);

                        if (writer != null)
                            writer.Write(arr);

                        ind = 0;
                    }
                }
                else if (!write_sign && Helper.ring_buffer[m.ip.Address.ToString()].Count == 0 && dr.f_reset_ring_buffer == false)
                {
                    lock (locker) { dr.f_reset_ring_buffer = true; }

                    while (forever)
                    {
                        // Идентификационный номер сигнала
                        id = BitConverter.ToUInt32(data, num);
                        num += 4;
                        if (id == 0 || num >= data.Length) break;

                        // Входа для ЦП или выхода для модуля
                        if (id > 1000000)
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id >= 1000016)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                                else if (id == 1000014)
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.mod.Parameters[3].Parameter[13].datatype);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);

                                    m.pr.out_signals[j].ReInit(tempr2);

                                    values.Add(Helper.GetInstVal(tempr2));

                                    num += size;
                                }
                                else if (id == 1000015)
                                {
                                    j++;

                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                                else
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                            }
                            else
                            {
                                if (id - 1000000 > m.pr.out_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }

                                num++;
                                m.pr.out_signals[j].id = id;

                                size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                tempr2 = new byte[size];
                                Array.Copy(data, num, tempr2, 0, size);
                                m.pr.out_signals[j].ReInit(tempr2);

                                j++;

                                num += size;
                            }
                        }
                        // Выхода для ЦП или входа для модуля
                        else
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id > 1000015)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }
                            else
                            {
                                if (id - 1000000 > m.pr.in_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }

                            num++;
                            m.pr.in_signals[i].id = id;

                            size = Helper.SizeOfType(m.pr.in_signals[i].data_type);
                            tempr2 = new byte[size];
                            Array.Copy(data, num, tempr2, 0, size);
                            m.pr.in_signals[i].ReInit(tempr2);
                            i++;
                            num += size;
                        }
                    }

                    if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                    {
                        foreach (byte[][] p in values)
                        {
                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq).Length;

                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq++).Length;

                            for (int n = 0; n < 12; n++)
                            {
                                Array.Copy(p[n], 0, tempr, ind, 4);
                                ind += 4;

                                Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                                ind += 4;
                            }

                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[12].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;

                            arr = new byte[ind];
                            Array.Copy(tempr, 0, arr, 0, ind);

                            if (writer != null)
                                writer.Write(arr);
                            else
                                dr.f_reset_ring_buffer = true;



                            ind = 0;
                        }
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TD9"))
                    {
                        for (int ii = 0; ii < m.pr.in_signals.Count; ii++)
                        {
                            if (m.pr.in_signals[ii].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.in_signals[ii].id.ToString() + "\t" + m.pr.in_signals[ii].children[2].value +
                                    "\t" + m.pr.in_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].f_has_changed = false;
                                }
                            }

                            if (m.pr.out_signals[ii].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.out_signals[ii].id.ToString() + "\t" + m.pr.out_signals[ii].children[2].value +
                                    "\t" + m.pr.out_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].f_has_changed = false;
                                }
                            }
                        }
                        if (file_for_TD9 != null)
                            file_for_TD9.Write(cfg);
                        else
                            dr.f_reset_ring_buffer = true;

                        cfg = null;
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA902"))
                    {
                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq).Length;

                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq++).Length;

                        for (int n = 0; n < 6; n++)
                        {
                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;
                        }

                        arr = new byte[ind];
                        Array.Copy(tempr, 0, arr, 0, ind);

                        if (writer != null)
                            writer.Write(arr);

                        ind = 0;
                    }
                }
                else if (!write_sign && Helper.ring_buffer[m.ip.Address.ToString()].Count > 0 && dr.f_reset_ring_buffer == false)
                {

                    lock (locker) { Helper.dictionary_enable[Encoding.ASCII.GetBytes(m.pr.ident_collect[1].value)[0]] = false; }

                    while (forever)
                    {
                        // Идентификационный номер сигнала
                        id = BitConverter.ToUInt32(data, num);
                        num += 4;
                        if (id == 0 || num >= data.Length) break;

                        // Входа для ЦП или выхода для модуля
                        if (id > 1000000)
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id >= 1000016)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                                else if (id == 1000014)
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.mod.Parameters[3].Parameter[13].datatype);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);

                                    m.pr.out_signals[j].ReInit(tempr2);

                                    values.Add(Helper.GetInstVal(tempr2));

                                    num += size;
                                }
                                else if (id == 1000015)
                                {
                                    j++;

                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                                else
                                {
                                    num++;
                                    m.pr.out_signals[j].id = id;

                                    size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                    tempr2 = new byte[size];
                                    Array.Copy(data, num, tempr2, 0, size);
                                    m.pr.out_signals[j].ReInit(tempr2);

                                    j++;

                                    num += size;
                                }
                            }
                            else
                            {
                                if (id - 1000000 > m.pr.out_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }

                                num++;
                                m.pr.out_signals[j].id = id;

                                size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                                tempr2 = new byte[size];
                                Array.Copy(data, num, tempr2, 0, size);
                                m.pr.out_signals[j].ReInit(tempr2);

                                j++;

                                num += size;
                            }
                        }
                        // Выхода для ЦП или входа для модуля
                        else
                        {
                            if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                            {
                                if (id > 1000015)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }
                            else
                            {
                                if (id - 1000000 > m.pr.in_signals.Count)
                                {
                                    num += data[num] + 1;
                                    continue;
                                }
                            }

                            num++;
                            m.pr.in_signals[i].id = id;

                            size = Helper.SizeOfType(m.pr.in_signals[i].data_type);
                            tempr2 = new byte[size];
                            Array.Copy(data, num, tempr2, 0, size);
                            m.pr.in_signals[i].ReInit(tempr2);
                            i++;
                            num += size;
                        }
                    }

                    if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                    {
                        foreach (byte[][] p in values)
                        {
                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq).Length;

                            Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                            ind += BitConverter.GetBytes(seq++).Length;

                            for (int n = 0; n < 12; n++)
                            {
                                Array.Copy(p[n], 0, tempr, ind, 4);
                                ind += 4;

                                Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                                ind += 4;
                            }

                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[12].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;

                            arr = new byte[ind];
                            Array.Copy(tempr, 0, arr, 0, ind);

                            if (writer != null)
                                writer.Write(arr);
                            else
                                dr.f_reset_ring_buffer = true;



                            ind = 0;
                        }
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TD9"))
                    {
                        for (int ii = 0; ii < m.pr.in_signals.Count; ii++)
                        {
                            if (m.pr.in_signals[ii].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.in_signals[ii].id.ToString() + "\t" + m.pr.in_signals[ii].children[2].value +
                                    "\t" + m.pr.in_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].f_has_changed = false;
                                }
                            }

                            if (m.pr.out_signals[ii].f_has_changed)
                            {
                                lock (locker)
                                {
                                    cfg += m.pr.out_signals[ii].id.ToString() + "\t" + m.pr.out_signals[ii].children[2].value +
                                    "\t" + m.pr.out_signals[ii].children[3].value + "\n";
                                    m.pr.in_signals[ii].f_has_changed = false;
                                }
                            }
                        }
                        if (file_for_TD9 != null)
                            file_for_TD9.Write(cfg);
                        else
                            dr.f_reset_ring_buffer = true;

                        cfg = null;
                    }
                    else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA902"))
                    {
                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq).Length;

                        Array.Copy(Helper.InvertArray(BitConverter.GetBytes(seq + 9)), 0, tempr, ind, BitConverter.GetBytes(seq).Length);
                        ind += BitConverter.GetBytes(seq++).Length;

                        for (int n = 0; n < 6; n++)
                        {
                            Array.Copy(BitConverter.GetBytes(Single.Parse(m.pr.out_signals[n].children[2].value)), 0, tempr, ind, 4);
                            ind += 4;
                        }

                        arr = new byte[ind];
                        Array.Copy(tempr, 0, arr, 0, ind);

                        if (writer != null)
                            writer.Write(arr);

                        ind = 0;
                    }
                }
                else
                {
                    if (dr.f_reset_ring_buffer == false)
                    {
                        dr.f_reset_ring_buffer = true;
                        Helper.dictionary_enable[Encoding.ASCII.GetBytes(m.pr.ident_collect[1].value)[0]] = true;
                    }

                    if (writer != null)
                    {
                        if (cfg != null)
                            fStream.Write(cfg);
                        writer.Close();
                        fStream.Close();
                        writer = null;
                        fStream = null;
                        cfg = null;
                    }

                    if (file_for_TD9 != null)
                    {
                        if (cfg != null)
                            file_for_TD9.Write(cfg);
                        file_for_TD9.Close();
                        file_for_TD9 = null;
                        cfg = null;
                    }


                    while (forever)
                    {
                        if (num >= data.Length) break;

                        // Идентификационный номер сигнала
                        id = BitConverter.ToUInt32(data, num);

                        num += 4;

                        if (id == 0) break;

                        // Входа модуля
                        if (id > 1000000)
                        {
                            if (id - 1000000 > m.pr.out_signals.Count)
                            {
                                num += data[num] + 1;
                                continue;
                            }
                            num++;
                            if (id == 1000015)
                                j++;

                            m.pr.out_signals[j].id = id;

                            size = Helper.SizeOfType(m.pr.out_signals[j].data_type);
                            tempr2 = new byte[size];
                            Array.Copy(data, num, tempr2, 0, size);

                            m.pr.out_signals[j].ReInit(tempr2);

                            if (id != 1000014)
                                j++;
                            num += size;
                        }
                        // Выхода модуля
                        else
                        {
                            if (id > m.pr.in_signals.Count)
                            {
                                num += data[num] + 1;
                                continue;
                            }
                            num++;
                            m.pr.in_signals[i].id = id;

                            size = Helper.SizeOfType(m.pr.in_signals[i].data_type);
                            tempr2 = new byte[size];
                            Array.Copy(data, num, tempr2, 0, size);
                            m.pr.in_signals[i].ReInit(tempr2);
                            i++;
                            num += size;
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }
    }
}
