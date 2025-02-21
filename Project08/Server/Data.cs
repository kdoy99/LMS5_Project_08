using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Data
    {
        // 메모리
        public double TotalMemory { get; set; } // 총 메모리
        public double FreeMemory { get; set; } // 남은 메모리
        public double RemainMemory { get; set; } // 사용중인 메모리        

        // CPU
        public float cpuValueData { get; set; } // CPU 사용량

        // HDD
        public string HDDname { get; set; }
        public long totalSize { get; set; }
        public long freeSize { get; set; }
        public long useSize { get; set; }
    }
}
