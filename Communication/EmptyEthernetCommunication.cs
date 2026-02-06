using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    // 空以太网实例（占位用）
    // 空以太网实例（占位用）
    public class EmptyEthernetCommunication : ICommunication
    {
        public bool IsConnected => false;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public CommunicationConfig Config { get; set; }

        public void Open(CommunicationConfig config) { }
        public void Close() { }
        public void Send(byte[] data, int offset, int length) { }
        public string FormatSendData(byte[] data, int length) => string.Empty;
        public void Dispose() { }
    }
}
