﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    public delegate void AddModuleDelegate(byte[] data, IPEndPoint ip);
    public delegate int EventHandler(byte[] data, IPEndPoint ip);
    public delegate void EventHandlerReady(byte[] data, IPEndPoint ip);
    public delegate void EventHandlerLC(IPEndPoint ip, bool state);
    public delegate void UpdateDelegate(IPEndPoint ip);
    public delegate void UpdateUPD();
    public delegate void Flag();
}
