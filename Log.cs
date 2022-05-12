using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    public class Logging
    {
        public Logging()
        {
        }

        byte mask { get; set; }

        string description { get; set; }

        DateTime timestamp { get; set; }

        public void Log(byte mask, string description, DateTime timestamp)
        {
            
        }

        private List<string> _history = new List<string>();
        public List<string> history 
        {
            get { return _history; }
            set { _history = value; }
        }
    }
}
