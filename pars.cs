using checkmod.TreeGrid;
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
    /// <summary>
    /// Класс, описывающий Элемент структуры
    /// </summary>
    public class TypeParam : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Интервал времени
        TimeSpan ts;

        // Обновление коллекции
        public UpdateUPD UPD = null;

        // Флаг возможности изменения параметра
        public bool f_has_changed = false;

        // Метка времени, отсчет от 1 января 1970 года
        DateTime dt = new DateTime(1970, 1, 1);

        // Объект структуры параметра
        public PLCHWModulesModuleSectionParameter param;

        // Закодированное значение 0 и 1
        byte[] array_zero = BitConverter.GetBytes((UInt32)16733525), array_one = BitConverter.GetBytes((UInt32)4278233770);

        // Значение
        private string _value;
        public string value
        {
            get
            {
                // Проверка на вложенность
                if (children != null && children.Count > 0)
                    // Нахождение элемента с именем Value
                    if (children.Any(x => x.name == "Value"))
                        // Проверка крайний ли это элемент во вложенности
                        if (children.Single(x => x.name == "Value").children == null)
                            // Возврат значения
                            return children.Single(x => x.name == "Value").value;
                return _value;
            }

            set
            {
                if (value != _value)
                {
                    _value = value;
                    if (UPD != null)
                        UPD();
                }
            }
        }

        // Индификатор параметра
        public UInt32 id { get; set; }

        // Минимальное значение
        public string min { get; set; }

        // Максимальное значение
        public string max { get; set; }

        // Является ли данная структура выходным сигналом
        private Visibility _is_value = Visibility.Collapsed;
        public Visibility is_value 
        { 
            get 
            { 
                return _is_value; 
            }

            set
            {
                _is_value = value;
            }
        }

        // Имя
        public string name { get; set; }

        // Предустановленное значение
        public string tempr { get; set; }

        // Дополнительная опция параметра
        public string options { get; set; }

        // Атрибут возможности чтения/записи
        public string online_access { get; set; }

        // Тип данных
        public string data_type { get; set; }

        // Описание
        public string description { get; set; }

        /// Возможность редактирования
        public bool isEditable { get; set; } = false;

        // Вложенные элементы
        public ObservableCollection<TypeParam> children { get; set; } = null;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="Parameter"> Структура параметра </param>
        /// <param name="value"> Значение параметра </param>
        public TypeParam(PLCHWModulesModuleSectionParameter Parameter, byte[] value = null)
        {
            param = Parameter;
            MinMax range = null;
            this.name = Parameter.Name?.Trim('\'', '\"');
            this.data_type = Parameter.datatype?.Trim('\'', '\"');
            this.description = Parameter.Description?.Trim('\'', '\"');
            
            // Проверка наличия максимального и миниманого значения в структуре
            if (!(string.IsNullOrEmpty(Parameter.Min) || string.IsNullOrEmpty(Parameter.Max)))
            {
                min = Parameter.Min?.Trim('\'', '\"');
                max = Parameter.Max?.Trim('\'', '\"');
            }
            else
            { 
                // Получение типа параметра
                range = Helper.MinMaxOfType(Parameter.datatype?.Trim('\'', '\"'));
                min = range.min;
                max = range.max;
            }

            // Пороверка на вложенность
            if (Parameter.Value.Element == null)
            { 
                // Задание возможности редактирования
                isEditable = true;

                // Если отсутствует значение параметра, то подставляется из XML
                if (value == null)
                    this.value = Parameter.Value.Text[0]?.Trim('\'', '\"');
                else
                    this.value = Encoding.ASCII.GetString(value).Trim('\'', '\"');
            }
            else
            {
                TypeParam child = null;

                // Перебор элементов на вложенность
                foreach (PLCHWModulesModuleSectionParameterValueElement p in Parameter.Value.Element)
                {
                    if (p.Element == null)
                        child = new TypeParam(p, Encoding.ASCII.GetBytes(p.Text[0]?.Trim('\'', '\"')));
                    else
                    {
                        if (value == null)
                            child = new TypeParam(p);
                        else
                            child = new TypeParam(p, value);
                    }

                    if (children == null)
                        children = new ObservableCollection<TypeParam>();

                    children.Add(child);
                }
            }

            // Является ли эта структура выходным сигналом
            is_value = IsValue();
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="Parameter"> Структура элемента параметра </param>
        /// <param name="value"> Значение параметра </param>
        public TypeParam(PLCHWModulesModuleSectionParameterValueElement Parameter, byte[] value = null)
        {
            // диапазон значения
            MinMax range = null;

            this.options = Parameter.options;
            this.online_access = Parameter.onlineaccess;
            this.name = Parameter.name?.Trim('\'', '\"');
            this.data_type = Parameter.basetype?.Trim('\'', '\"');
            this.description = Parameter.description?.Trim('\'', '\"');
           
            // Проверка наличия максимального и миниманого значения в структуре
            if (!(string.IsNullOrEmpty(Parameter.min) || string.IsNullOrEmpty(Parameter.max)))
            {
                // Удаление из строки одинарных и двойных ковычек
                min = Parameter.min?.Trim('\'', '\"');
                max = Parameter.max?.Trim('\'', '\"');
            }
            else
            {
                // Получение диапазона значения
                range = Helper.MinMaxOfType(Parameter.basetype?.Trim('\'', '\"'));
                min = range.min;
                max = range.max;
            }

            // Пороверка на вложенность
            if (Parameter.Element == null)
            {
                // Возможность редактирования
                this.isEditable = true;

                // Проверка наличие значения
                if (value == null)
                    this.value = Parameter.Text[0]?.Trim('\'', '\"');   // Берем значение из XML 
                else
                    this.value = Encoding.ASCII.GetString(value);
            } 
            else
            {
                // Размер вложенного элемента
                byte s;

                // Буфер со вложенными значениями
                byte[] arr;

                // Элемент структуры данных
                TypeParam child = null;

                // Перебор элементов на вложенность
                foreach (PLCHWModulesModuleSectionParameterValueElement p in Parameter.Element)
                {
                    // Получение размера структуры
                    s = Helper.SizeOfType(p.basetype);

                    // Выделение памяти
                    arr = new byte[s];

                    // Проверка на наличие значения
                    if (value == null)
                        this.value = p.Text[0]?.Trim('\'', '\"');   // Берем значение из XML
                    else
                    { 
                        // Копируем полученные данные в выделеную память
                        Array.Copy(value, 0, arr, 0, s);
                        Array.Copy(value, s, value, 0, value.Length - s);

                        // Изменяем размер буфера полученных данных
                        Array.Resize(ref value, value.Length - s);

                        // Запись значения в элемент структуры
                        this.value = Encoding.ASCII.GetString(arr);
                    }

                    // Инициализация элемента структуры
                    child = new TypeParam(p, value);

                    // Инициализация коллекции с элементами данных
                    if (children == null)
                        children = new ObservableCollection<TypeParam>();

                    // Добавление в коллекцию элемента данных
                    children.Add(child);
                }
            }
        }

        /// <summary>
        /// Переинициализация параметра
        /// </summary>
        /// <param name="value"> Значение элементов структуры </param>
        public void ReInit(byte[] value)
        {
            // arr - Буфер с длиной структуры элемента
            // this_value - Буфер со всеми данными; 
            byte[] arr, this_value = new byte[value.Length];

            // Перекладываем данные во временный буфер
            Array.Copy(value, 0, this_value, 0, this_value.Length);
            
            // Проверка на вложенность
            if (param.Value.Element == null)
            {
                this.value = Helper.GetString(param.datatype, this_value);
            }
            else
            {
                // Перебор элементов на вложенность
                foreach (PLCHWModulesModuleSectionParameterValueElement p in param.Value.Element)
                {
                    // Инициализируем значение структуры элемента
                    arr = new byte[p.size];
                    Array.Copy(this_value, 0, arr, 0, arr.Length);
                    Array.Copy(this_value, arr.Length, this_value, 0, this_value.Length - arr.Length);

                    // Изменяем размер временного буфера
                    Array.Resize(ref this_value, this_value.Length - arr.Length);

                    // ВЫзов метода преинициализации элемента параметра
                    ReSubInit(p, this.children.Single(x => x.name.Equals(p.name)), arr);
                }
            }
        }

        /// <summary>
        /// Переинициализация элемента параметра
        /// </summary>
        /// <param name="p"> Элемент параметра </param>
        /// <param name="param"> Структура параметра </param>
        /// <param name="value"> Значение элемента параметра </param>
        public void ReSubInit(PLCHWModulesModuleSectionParameterValueElement p, TypeParam param, byte[] value)
        {
            // time - количество секунд
            // sec - количество наносекунд
            UInt32 time, sec;

            // arr - Буфер с длиной структуры элемента
            // after_sec - Буфер со значением количества секунд 
            // before_sec - Буфер со значением количества наносекунд 
            byte[] arr, after_sec = new byte[4], before_sec = new byte[4];

            // Проверка на вложенность
            if (p.Element == null)
            {
                // Проверка значения атрибута options
                if (!(string.IsNullOrEmpty(param.options)) && (param.options == "CODING"))
                {
                    // Раскодирование значения
                    if (Enumerable.SequenceEqual(value, array_one))
                    { 
                        if (param.value != "1")
                        {
                            param.value = "1";
                            param.f_has_changed = true;
                        }
                    }
                    else if (Enumerable.SequenceEqual(value, array_zero))
                        if (param.value != "0")
                        {
                            param.value = "0";
                            param.f_has_changed = true;
                        }
                }
                // Проверка имени на значение "Time"
                else if (p.name == "Time")
                {
                    // Парсинг метки времени
                    Array.Copy(value, 0, after_sec, 0, after_sec.Length);
                    Array.Copy(value, 4, before_sec, 0, before_sec.Length);
                    time = BitConverter.ToUInt32(after_sec,0);
                    sec = BitConverter.ToUInt32(before_sec, 0);

                    if (time != 0)
                    {
                        ts = TimeSpan.FromSeconds(time);
                        param.value = (dt + ts).ToString() + "." + sec.ToString();
                    }
                    else
                        param.value = "0";


                }    
                else
                    param.value = Helper.GetString(p.basetype, value);
            }
            else
                foreach (PLCHWModulesModuleSectionParameterValueElement pp in p.Element)
                {
                    arr = new byte[pp.size];
                    Array.Copy(value, 0, arr, 0, arr.Length);
                    Array.Copy(value, arr.Length, value, 0, value.Length - arr.Length);
                    Array.Resize(ref value, value.Length - arr.Length);
                    ReSubInit(pp, param.children.Single(x => x.name.Equals(pp.name)), arr);
                }
        }

        /// <summary>
        ///  Очистка временного значения параметра
        /// </summary>
        public void ClearTempr()
        {
            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
                foreach (TypeParam p in children)
                    ClearSubTempr(p);
            else
                tempr = null;

            // Обновление графики
            if (UPD != null)
                UPD();
        }

        /// <summary>
        /// Очистка временного значения параметра элемента
        /// </summary>
        /// <param name="p"> Элемент параметра </param>
        public void ClearSubTempr(TypeParam p)
        {
            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
                foreach (TypeParam pp in p.children)
                    ClearSubTempr(pp);
            else
                p.tempr = null;

            // Обновление графики
            if (p.UPD != null)
                p.UPD();
        }

        /// <summary>
        /// Возрат временного или постоянного значения для отправки данных
        /// </summary>
        /// <returns> Масиив со значением </returns>
        public byte[] SetValue()
        {
            // Длина значения
            int cnt = 0;

            // Позиция и длина значения в структуре параметра
            int[] val = null;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
                foreach (TypeParam p in children)
                {
                    temp = SetSubValue(p);

                    if (temp != null)
                    { 
                        cnt += temp.Length;
                        Array.Resize(ref data, cnt);
                        Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                    }
                    else
                        return null;
                }
            else
                // Проверка на наличие временного, постоянного значения
                if (!string.IsNullOrEmpty(tempr))
                    data = Helper.GetBytes(data_type, tempr.Trim('\'', '\"'));
                else if (!string.IsNullOrEmpty(value))
                    data = Helper.GetBytes(data_type, value.Trim('\'', '\"'));
                else
                    data = Helper.GetBytes(data_type, "0");

            // Проверка типа параметра
            if (Helper.send_value.ContainsKey("DSIGNAL_T"))
            {
                // Запись знаечнния в массив
                if (Helper.send_value.TryGetValue("DSIGNAL_T", out val))
                { 
                    Array.Copy(data, val[0], data, 0, val[1]);
                    Array.Resize(ref data, val[1]);
                }
            }

            // Обновление графики
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Возрат временного или постоянного значения элемента параметра для отправки данных
        /// </summary>
        /// <param name="p"> Элемент параметра </param>
        /// <returns> Массив со значением </returns>
        public byte[] SetSubValue(TypeParam p)
        {
            // Длина значения
            int cnt = 0;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
                foreach (TypeParam pp in p.children)
                {
                    // Получение значения со следующего уровня вложенности
                    temp = SetSubValue(pp);
                    cnt += temp.Length;

                    // Изменение размера буфера с принятыми данными
                    Array.Resize(ref data, cnt);
                    Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                }
            else
            {
                // Проверка наличия временного значеия
                if (!string.IsNullOrEmpty(p.tempr))
                {
                    // Проверка атрибута options на значение CODING
                    if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")))
                    { 
                        // Кодировка значения
                        if (p.tempr.Trim('\'', '\"') == "1")
                            data = BitConverter.GetBytes((UInt32)4278233770);
                        else if (p.tempr.Trim('\'', '\"') == "0")
                            data = BitConverter.GetBytes((UInt32)16733525);
                    }    
                    else
                        // Взятие значения из XML
                        data = Helper.GetBytes(p.data_type, p.tempr.Trim('\'', '\"'));
                }
                else if (!string.IsNullOrEmpty(p.value))
                {
                    // Проверка атрибута options на значение CODING
                    if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")))
                    {
                        // Кодировка значения
                        if (p.value.Trim('\'', '\"') == "1")
                            data = BitConverter.GetBytes((UInt32)4278233770);
                        else if (p.value.Trim('\'', '\"') == "0")
                            data = BitConverter.GetBytes((UInt32)16733525);
                    }
                    else
                        // Взятие значения из XML
                        data = Helper.GetBytes(p.data_type, p.value.Trim('\'', '\"'));
                }
                else
                {
                    // Проверка атрибута options на значение CODING
                    if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")))
                    {
                        // Кодировка значения
                        if (p.value.Trim('\'', '\"') == "1")
                            data = BitConverter.GetBytes((UInt32)4278233770);
                        else if (p.value.Trim('\'', '\"') == "0")
                            data = BitConverter.GetBytes((UInt32)16733525);
                    }
                    else
                        // Взятие значения из XML
                        data = Helper.GetBytes(p.data_type, "0");
                }
            }

            // Обновление данных в окне
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Возрат нуля для отправки данных
        /// </summary>
        /// <returns> Масиив со значением </returns>
        public byte[] SetZero()
        {
            // Длина значения
            int cnt = 0;

            // Позиция и длина значения в структуре параметра
            int[] val = null;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
                foreach (TypeParam p in children)
                {
                    temp = SetSubZero(p);
                    cnt += temp.Length;
                    Array.Resize(ref data, cnt);
                    Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                }
            else
            {
                // Проверка атрибута options на значение CODING
                if (!(string.IsNullOrEmpty(options)) && (options.Contains("CODING")))
                {

                    data = BitConverter.GetBytes((UInt32)16733525);
                }
                else
                    data = Helper.GetBytes(data_type, "0");
            }

            // Проверка типа параметра
            if (Helper.send_value.ContainsKey("DSIGNAL_T"))
            {
                // Запись знаечнния в массив
                if (Helper.send_value.TryGetValue("DSIGNAL_T", out val))
                {
                    Array.Copy(data, val[0], data, 0, val[1]);
                    Array.Resize(ref data, val[1]);
                }
            }

            // Обновление графики
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Возрат нулевого значения элемента для отправки данных
        /// </summary>
        /// <param name="p"> Элемент параметра </param>
        /// <returns> Массив со значением </returns>
        public byte[] SetSubZero(TypeParam p)
        {
            // Длина значения
            int cnt = 0;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
                foreach (TypeParam pp in p.children)
                {
                    // Получение значения со следующего уровня вложенности
                    temp = SetSubZero(pp);
                    cnt += temp.Length;

                    // Изменение размера буфера с принятыми данными
                    Array.Resize(ref data, cnt);
                    Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                }
            else
            {
                // Проверка атрибута options на значение CODING
                if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")))
                {
                    
                    data = BitConverter.GetBytes((UInt32)16733525);
                }
                else
                    data = Helper.GetBytes(p.data_type, "0");
            }    
                

            // Обновление данных в окне
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Возрат единицы для отправки данных
        /// </summary>
        /// <returns> Масиив со значением </returns>
        public byte[] SetOne()
        {
            // Длина значения
            int cnt = 0;

            // Позиция и длина значения в структуре параметра
            int[] val = null;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
                foreach (TypeParam p in children)
                {
                    temp = SetSubOne(p);
                    cnt += temp.Length;
                    Array.Resize(ref data, cnt);
                    Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                }
            else
            {
                // Проверка атрибута options на значение CODING
                if (!(string.IsNullOrEmpty(options)) && (options.Contains("CODING")))
                {

                    data = BitConverter.GetBytes((UInt32)4278233770);
                }
                else
                    data = Helper.GetBytes(data_type, "1");
            }

            // Проверка типа параметра
            if (Helper.send_value.ContainsKey("DSIGNAL_T"))
            {
                // Запись значения в массив
                if (Helper.send_value.TryGetValue("DSIGNAL_T", out val))
                {
                    Array.Copy(data, val[0], data, 0, val[1]);
                    Array.Resize(ref data, val[1]);
                }
            }

            // Обновление графики
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Возрат единичного значения элемента для отправки данных
        /// </summary>
        /// <param name="p"> Элемент параметра </param>
        /// <returns> Массив со значением </returns>
        public byte[] SetSubOne(TypeParam p)
        {
            // Длина значения
            int cnt = 0;

            // temp - Значение элемента параметра
            // data - значение параметра
            byte[] temp, data = null;

            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
                foreach (TypeParam pp in p.children)
                {
                    // Получение значения со следующего уровня вложенности
                    temp = SetSubOne(pp);
                    cnt += temp.Length;

                    // Изменение размера буфера с принятыми данными
                    Array.Resize(ref data, cnt);
                    Array.Copy(temp, 0, data, cnt - temp.Length, temp.Length);
                }
            else
            {
                // Проверка атрибута options на значение CODING
                if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")))
                {

                    data = BitConverter.GetBytes((UInt32)4278233770);
                }
                else
                    data = Helper.GetBytes(p.data_type, "1");
            }

            // Обновление данных в окне
            if (UPD != null)
                UPD();

            return data;
        }

        /// <summary>
        /// Преобразование значения параметра в массив данных
        /// </summary>
        /// <returns> Возвращает значение в виде массив байт </returns>
        public byte[] GetBytes()
        {
            // tempr - Временный буфер
            // data - Буфер с данными
            byte[] tempr, data = null;

            // cnt - Длина буфера (временный + постоянный)
            // start - Длина буфера (постоянный)
            int cnt = 0, start = 0;

            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
            {
                // Перебор вложенных элементов
                foreach (TypeParam p in children)
                {
                    // Получение буфера с данными элемента
                    tempr = GetSubByte(p);

                    if (tempr == null)
                        return null;
                    
                    cnt += tempr.Length;

                    // Изменение размера буфера с полученными данными
                    Array.Resize(ref data, cnt);
                    Array.Copy(tempr, 0, data, start, tempr.Length);

                    start = cnt;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(value))
                    return null;

                // Запись данных в буфер
                data = Helper.GetBytes(data_type, value.Trim('\'', '\"'));
            }

            return data;
        }

        /// <summary>
        /// Преобразование значения элемента параметра в массив данных
        /// </summary>
        /// <param name="p"></param>
        /// <returns> Возвращает значение в виде массив байт </returns>
        public byte[] GetSubByte(TypeParam p)
        {
            // tempr - Временный буфер
            // data - Буфер с данными
            byte[] tempr, data = null;

            // cnt - Длина буфера (временный + постоянный)
            // start - Длина буфера (постоянный)
            int cnt = 0, start = 0;

            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
            {
                // Перебор вложенных элементов
                foreach (TypeParam pp in p.children)
                {
                    // Получение буфера с данными элемента
                    tempr = GetSubByte(pp);

                    cnt += tempr.Length;

                    // Изменение размера буфера с полученными данными
                    Array.Resize(ref data, cnt);
                    Array.Copy(tempr, 0, data, start, tempr.Length);

                    start = cnt;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(p.value.Trim('\'', '\"')))
                    return null;

                // Запись данных в буфер
                data = Helper.GetBytes(p.data_type, p.value.Trim('\'', '\"'));// Encoding.ASCII.GetBytes(p.value.Trim('\'', '\"'));
            }
            return data;
        }

        /// <summary>
        /// Является ли данная структура выходным дискретным сигналом 
        /// </summary>
        /// <returns></returns>
        public Visibility IsValue()
        {
            // Проверка на вложенность
            if ((children != null) && (children.Count > 0))
            {
                // Перебор вложенных элементов
                foreach (TypeParam p in children)
                {
                    // Проверка атрибута options на значение CODING и online_access на значение RW
                    if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")) && !(string.IsNullOrEmpty(p.online_access)) && (p.online_access.Contains("RW")))
                        return (Visibility)Visibility.Visible;

                    // Является ли элемент частью структуры выходного сигнала
                    if (IsSubValue(p) == (Visibility)Visibility.Visible)
                        return (Visibility)Visibility.Visible;
                }
            }
            else
                // Проверка атрибута options на значение CODING и online_access на значение RW
                if (!(string.IsNullOrEmpty(options)) && (options.Contains("CODING")) && !(string.IsNullOrEmpty(online_access)) && (online_access.Contains("RW")))
                    return (Visibility)Visibility.Visible;

            return (Visibility)Visibility.Collapsed;
        }

        /// <summary>
        /// Является ли данный элемент структуры выходным дискретным сигналом 
        /// </summary>
        /// <returns></returns>
        public Visibility IsSubValue(TypeParam p)
        {
            // Проверка на вложенность
            if ((p.children != null) && (p.children.Count > 0))
            {
                // Перебор вложенных элементов
                foreach (TypeParam pp in p.children)
                {
                    // Проверка атрибута options на значение CODING и online_access на значение RW
                    if (!(string.IsNullOrEmpty(pp.options)) && (pp.options.Contains("CODING")) && !(string.IsNullOrEmpty(pp.online_access)) && (pp.online_access.Contains("RW")))
                        return (Visibility)Visibility.Visible;

                    // Является ли элемент частью структуры выходного сигнала
                    if (IsSubValue(pp) == (Visibility)Visibility.Visible)
                        return (Visibility)Visibility.Visible;
                }
            }
            else
                // Проверка атрибута options на значение CODING и online_access на значение RW
                if (!(string.IsNullOrEmpty(p.options)) && (p.options.Contains("CODING")) && !(string.IsNullOrEmpty(p.online_access)) && (p.online_access.Contains("RW")))
                    return (Visibility)Visibility.Visible;

            return (Visibility)Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Класс, являющийся проекцией элементра данных в окне
    /// </summary>
    public class DataGridElement : TreeGridElement, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Параметр
        public TypeParam p { get; set; }

        // Имя
        public string name { get { return p.name; } set { } }

        // Значение 
        public string value { get { return p.value; } set { p.value = value; } }

        // Значение max
        public string value_max { get { return p.max; }}

        // Возможность редактирования
        public bool isEditable { get { return p.isEditable; }}

        // Предустановленное значение
        public string tempr { get { return p.tempr; } set { p.tempr = value; } }

        // Является ли структурой выходного сигнала
        public Visibility is_value { get { return p.is_value; } set { p.is_value = value; } }

        // Значение min 
        public string value_min { get { return p.min; }}

        // Описание
        public string description { get { return p.description; } set { } }

        // Тип данных
        public string data_type { get { return p.data_type; } set { } }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="name"> Имя элемента </param>
        /// <param name="description"> Описание элемента </param>
        /// <param name="data_type"> Тип элемента </param>
        /// <param name="string_value"> Значение элемента </param>
        /// <param name="Haschildren"> Вложенность </param>
        public DataGridElement(string name, string description, string data_type, string string_value = "", bool Haschildren = false)
        {
            // Инициализация таймера обновления графики
            if (HeaderDriver.TimerUpdate == null)
                HeaderDriver.TimerUpdate = new System.Timers.Timer(HeaderDriver.change_time);

            // Проверка нахождения таймера в словаре параметров
            if (!HeaderDriver.params_for_driver.ContainsKey("time_change_value"))
                HeaderDriver.params_for_driver.Add("time_change_value", HeaderDriver.TimerUpdate);

            this.name = name;
            this.value = string_value;
            this.data_type = data_type;
            this.description = description;
            HasChildren = Haschildren;

            // Переинициализация параметров таймера
            Update();
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="p"> Имя элемента </param>
        /// <param name="Haschildren"> Вложенность </param>
        public DataGridElement(TypeParam p, bool Haschildren = false)
        {
            // Инициализация таймера обновления графики
            if (HeaderDriver.TimerUpdate == null)
                HeaderDriver.TimerUpdate = new System.Timers.Timer(HeaderDriver.change_time);

            // Проверка нахождения таймера в словаре параметров
            if (!HeaderDriver.params_for_driver.ContainsKey("time_change_value"))
                HeaderDriver.params_for_driver.Add("time_change_value", HeaderDriver.TimerUpdate);

            this.p = p;
            this.p.UPD = Update;

            // Переинициализация параметров таймера
            Update();

            HasChildren = Haschildren;
        }

        /// <summary>
        /// Задание параметров таймеру обновления графики
        /// </summary>
        public void Update()
        {
            
            // Проверка на задания события таймеру
            if (!HeaderDriver.TimerUpdate.Enabled)
            {
                HeaderDriver.TimerUpdate.AutoReset = true;
                HeaderDriver.TimerUpdate.Elapsed += OnTimedEvent;
                HeaderDriver.TimerUpdate.Start();
            }
            else
            {
                HeaderDriver.TimerUpdate.Stop();
                HeaderDriver.TimerUpdate.Elapsed += OnTimedEvent;
                HeaderDriver.TimerUpdate.Start();
            }
        }

        /// <summary>
        /// Событие обновления графики
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Проверка на конфигурационный параметр
           /* if (p.param != null && p.param.parametertype != "none")
            {*/
                OnPropChanged("value");
                OnPropChanged("tempr");
            HeaderDriver.TimerUpdate.Elapsed -= OnTimedEvent;
            //}
        }
    }

    /// <summary>
    /// Класс заполнения коллекций с конфигурацией, входными и выходными сигналами
    /// </summary>
    public class pars : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropChanged(string propName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion // INotifyPropertyChanged Members

        // Значение индекса байта в буфере данных
        int i;

        // Экземпляр драйвера
        Driver dr;

        // Заводской номер
        public string plant_num;

        // Название идентификационных параметров
        string[] name_param = new string[12] { "Статус", "Позиция", "Тип модуля", "Численное исполнение модуля", "Имя модуля", "Имя программы",
                                               "Исполнение модуля", "Версия программы" , "Аппаратная версия", "Заводской номер",
                                               "Совместимость модуля", "Расширенная информация"};

        // Конфигурация
        private config _conf;
        public config conf { get => _conf; }
        
        // Сигналы
        private Signals _sign;
        public Signals sign { get => _sign; }

        // Модель древовидной разметки данных
        private TreeGridModel _PS = new TreeGridModel();
        public TreeGridModel PS
        {
            get { return _PS; }
            set { _PS = value; }
        }

        // Количество буферов в кольцевом буфере
        public string buffer_count
        {
            get 
            {
                if (Helper.ring_buffer.ContainsKey(dr.ip.Address.ToString()))
                    return Helper.ring_buffer[dr.ip.Address.ToString()].Count.ToString();
                return "Empty";
            }
        }

        // Коллекция с идентификационными данными
        public ObservableCollection<TypeParam> ident_collect { get; set; }

        // Коллекция, содержащая конфинурационные параметры
        private ObservableCollection<TypeParam> _conf_param = new ObservableCollection<TypeParam>();
        public ObservableCollection<TypeParam> conf_param
        {
            get { return _conf_param; }
            set { _conf_param = value; }
        }
            
        // Коллекция, содержащая входные сигналы
        private ObservableCollection<TypeParam> _in_signals = new ObservableCollection<TypeParam>();
        public ObservableCollection<TypeParam> in_signals
        {
            get { OnPropChanged("buffer_count"); return _in_signals; }
            set { OnPropChanged("buffer_count"); _in_signals = value;}
        }

        // Коллекция, содержащая выходные сигналы
        private ObservableCollection<TypeParam> _out_signals = new ObservableCollection<TypeParam>();
        public ObservableCollection<TypeParam> out_signals
        {
            get { OnPropChanged("buffer_count"); return _out_signals; }
            set { OnPropChanged("buffer_count"); _out_signals = value; }
        }

        // Свойство имени закладки
        private string header = "Empty";
        public string full_name { get {return header;} }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="dr"> Драйвер приложения </param>
        public pars(Driver dr)
        {
            this.dr = dr;
            _conf = new config(dr);
            _sign = new Signals(dr);
        }

        /// <summary>
        /// Метод поиска и проверки строкового параметра 
        /// </summary>
        /// <param name="data"> Буфер данных</param>
        public void check(byte[] data)
        {
            // Временный буфер
            byte[] tempr;

            // Идентификационный параметр
            TypeParam ident = null;

            // Задание позиции в буфере
            i = HeaderDriver.StartPosInIdentFrame;

            // Структура идентификационного параметра
            PLCHWModulesModuleSectionParameter param = null;

            //  Задание позиции отсчета в буфере
            int start = HeaderDriver.StartPosInIdentFrame;

            // Инициализация коллекции с идентификационными данными
            ident_collect = new ObservableCollection<TypeParam>();

            // Структура элемента данных
            PLCHWModulesModuleSectionParameterValue element = new PLCHWModulesModuleSectionParameterValue();

            // Инициализация идентификационного параметра и запись его в коллекцию
            for (int h = 0; h < 4; h++)
            {
                //Инициализируем параметр
                param = new PLCHWModulesModuleSectionParameter();

                // Заполняем параметр данными
                param.Name = name_param[h];
                param.datatype = "BYTE";
                param.Value = element;

                // Инициализируем идентификационный параметр
                ident = new TypeParam(param, new byte[] {0});

                // Задаем значение
                ident.value = data[h].ToString();

                // Добавляем в коллекцию
                ident_collect.Add(ident);
            }

            // Перебор идентификационных данных
            for (int j = 0; j <= 7; j++)
            {
                // Поиск строк в буфере
                for (; (char)data[i] != '\0'; i++)
                    ;

                // Инициализация временного буфера
                tempr = new byte[i - start];

                // Запись значения в буфер
                Array.Copy(data, start, tempr, 0, i - start);

                //Инициализируем параметр
                param = new PLCHWModulesModuleSectionParameter();

                // Заполняем параметр данными
                param.Value = element;
                param.Name = name_param[j + 4];
                param.datatype = "STRING";

                // Добавляем в коллекцию
                ident_collect.Add(new TypeParam(param, tempr));

                start = ++i;
            }

            // Заполняем заголовок модуля
            header = ident_collect[(byte)SeqIdentDataInCollect.modname].value + " " +
                ident_collect[(byte)SeqIdentDataInCollect.variation].value +
                "(" + ident_collect[(int)SeqIdentDataInCollect.position].value + ")";

            OnPropChanged("ident_collect");
        }
    }
}
