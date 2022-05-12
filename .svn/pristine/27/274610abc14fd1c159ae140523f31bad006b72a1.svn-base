using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    public class ParamConf
    {
        public ParamConf()
        { }

        public ParamConf(string ID, uint TOReset, uint Mode, uint Rate) //List<IdentBase> param)
        {
            this.ID = ID;
            this.TOReset = TOReset;
            this.Mode = Mode;
            this.Rate = Rate;
            //this.param = param;

            /* TOReset = (param[0] as IdentUint).value;
             Mode = (param[1] as IdentUint).value;
             Rate = (param[2] as IdentUint).value;*/
        }

        public string ID { get; set; }

        // Время до сброса модуля в отсутствии связи с ЦП после получения конфигурации, сек (0 – не срабатывать, от 1 до 500)
        public uint TOReset { get; set; }

        // Режим работы модуля: Бит 0 – Бит 15 – резерв
        public uint Mode { get; set; }

        // Период выдачи данных в ЦП, мкс с шагом 250 мкс
        public uint Rate { get; set; }

        // public List<IdentBase> param { get; set; }
    }
}
