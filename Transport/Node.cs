namespace Aaf.Sinc.Transport
{
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
        /// 别名
        /// </summary>
        public string Alias { get; set; }

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
            return string.Format("Alias:{0},ComputerName:{1},IP:{2},WorkGroup:{3},Online:{4}", Alias, ComputerName, IP, WorkGroup, Online);
        }
    }
}