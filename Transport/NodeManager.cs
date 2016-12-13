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

    public class Node
    {
        /// <summary>
        /// 主机名
        /// </summary>
        public string ComputerName { get; set; }
        /// <summary>
        /// IP
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// 自定义名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 工作组
        /// </summary>
        public string WorkGroup { get; set; }
        /// <summary>
        /// 在线状态
        /// </summary>
        public bool Online { get; set; }

        public override string ToString()
        {
            return string.Format("Name:{0},ComputerName:{1},IP:{2},WorkGroup:{3},Online:{4}", Name, ComputerName, IP, WorkGroup,Online);
        }
    }
}