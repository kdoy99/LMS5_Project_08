using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class ClientData
    {
        // 메모리
        public double TotalMemory { get; set; }
        public double FreeMemory { get; set; }
        public double RemainMemory { get; set; }        

        // CPU
        public float cpuValueData { get; set; }

        // HDD
        public string HDDname { get; set; }
        public long totalSize { get; set; }
        public long freeSize { get; set; }
        public long useSize { get; set; }

    }
}
