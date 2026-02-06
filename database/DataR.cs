using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace cross.database
{
    public struct DataR
    {
        public int SN, COMMAND, LENG, NO, TYP, ACK,RWX;
        public byte[] ByteArr; 
        public float VALUE, FACTOR;
        public string NAME, UNITor;
    }
}
