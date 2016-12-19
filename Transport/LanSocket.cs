using Aaf.Sinc.Utils;
using System.Net;
using System.Net.Sockets;

namespace Aaf.Sinc.Transport
{
    internal class LanSocket
    {
        private Socket socketSent;
        private IPEndPoint ipSent;

        public LanSocket(Socket socketSent, IPEndPoint ipSent)
        {
            this.socketSent = socketSent;
            this.ipSent = ipSent;
        }

        public void Connect()
        {
            socketSent.Connect(ipSent);
        }
    }
}