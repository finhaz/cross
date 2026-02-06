using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    // 协议实体类
    public class ProtocolInfo
    {
        public string Name { get; set; } // 协议名称
        public string FrameStructure { get; set; } // 报文结构
        public string Transport { get; set; } // 传输方式
        public string Features { get; set; } // 特点
    }

}
