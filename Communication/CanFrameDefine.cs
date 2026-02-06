using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    /// <summary>
    /// CAN帧实体 (对应DBC中的BO_)
    /// </summary>
    public class CanFrameDefine
    {
        public uint FrameId { get; set; }
        public string FrameName { get; set; }
        public byte DataLength { get; set; }
        public List<CanSignalDefine> Signals { get; set; } = new List<CanSignalDefine>();
    }

    public class CanSignalDefine
    {
        public string SignalName { get; set; }
        public int StartBit { get; set; }
        public int BitLength { get; set; }
        public int ByteOrder { get; set; }
        public string Sign { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public string Unit { get; set; }
        public double PhysicalValue { get; set; }
        public long RawValue { get; set; }
        public string UpdateTime { get; set; }
    }

}
