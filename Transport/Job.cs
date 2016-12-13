using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Aaf.Sinc.Transport
{
  
    public class Job
    {
        public string Cmd { get; set; }
        public string Path { get; set; }
        public string PathType { get; set; }
    }
}