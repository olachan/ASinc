using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Aaf.Sinc.Transport
{
    public static class NodeManager
    {
        private static ConcurrentDictionary<string, Node> nodes = new ConcurrentDictionary<string, Node>();

        public static bool Add(Node node)
        {
            if (nodes.ContainsKey(node.IP)) return false;
            return nodes.TryAdd(node.IP, node);
        }

        public static bool Remove(Node node)
        {
            return nodes.TryRemove(node.IP, out node);
        }

        public static int Count
        {
            get { return nodes.Count; }
        }

        public static List<string> IPs
        {
            get { return nodes.Keys.ToList(); }
        }
    }

    
}