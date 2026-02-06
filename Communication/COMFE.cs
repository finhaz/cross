using cross.database;
using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    //FE协议
    public class COMFE:IProtocol
    {
        // 单例模式（保持不变）
        private COMFE() { }
        private static readonly Lazy<COMFE> _instance = new Lazy<COMFE>(() => new COMFE());
        public static COMFE Instance => _instance.Value;


        //public byte[] sendbf = new byte[24];
        byte[] revbuffer = new byte[256];
        public void PSO_send(byte[] sendbf,float []u_g)
        {
            //将计算完成的数据存入发送数组中
            //回发数据为了保持两台机子同步接收，不区分逆变器，数据同时下发，逆变器侧自行根据位置进行接收
            Array.Clear(sendbf, 0, sendbf.Length);
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 19;//4个浮点数，16个字节，此外还有序列号+命令码+校验码，共19字节
            sendbf[5] = 200;
            sendbf[6] = 0xff;

            for (int i_n = 0; i_n < 4; i_n++)
            {
                byte[] temp_i = BitConverter.GetBytes(u_g[i_n]);
                sendbf[7 + 4 * i_n] = temp_i[0];
                sendbf[8 + 4 * i_n] = temp_i[1];
                sendbf[9 + 4 * i_n] = temp_i[2];
                sendbf[10 + 4 * i_n] = temp_i[3];
            }
            sendbf[23] = 0xff;

            //加5是还有包头4字节，以及长度19字节
            //serialPort1.Write(sendbf, 0, sendbf[4] + 5);
        }

        public void PSO_request(byte[] sendbf,byte Num_DSP)
        {
            int crc = 0;
            Array.Clear(sendbf, 0, sendbf.Length);
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 4; crc = sendbf[4];
            sendbf[5] = 200; crc = crc + sendbf[5];
            sendbf[6] = 0xff; crc = crc + sendbf[6];
            sendbf[7] = Num_DSP; crc = crc + sendbf[7];
            sendbf[8] = (byte)(0 - crc);
        }


        public void Monitor_Get(byte[] sendbf, byte sn, byte COMMAND)
        {
            int crc;
            Array.Clear(sendbf, 0, sendbf.Length);
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 3; crc = sendbf[4];
            sendbf[5] = sn; crc = crc + sendbf[5];
            sendbf[6] = COMMAND; crc = crc + sendbf[6];
            sendbf[7] = (byte)(0 - crc);
        }

        public void Monitor_Set(byte[] sendbf, byte sn,byte COMMAND,float send_value)
        {
            int crc = 0;
            Array.Clear(sendbf, 0, sendbf.Length);
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 7; crc = sendbf[4];
            sendbf[5] = sn; crc = crc + sendbf[5];
            sendbf[6] = (byte)(COMMAND); crc = crc + sendbf[6];

            byte[] temp_i = BitConverter.GetBytes(send_value);
            sendbf[7] = temp_i[0]; crc = crc + sendbf[7];
            sendbf[8] = temp_i[1]; crc = crc + sendbf[8];
            sendbf[9] = temp_i[2]; crc = crc + sendbf[9];
            sendbf[10] = temp_i[3]; crc = crc + sendbf[10];

            sendbf[11] = (byte)(0 - crc);
        }

        public void Monitor_Initial(byte[] sendbf,int sn1, float send_value)
        {
            int crc = 0;
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 7; crc = sendbf[4];
            sendbf[5] = (byte)sn1; crc = crc + sendbf[5];
            switch (sn1)
            {
                case 0: sendbf[6] = 175; break;
                case 1: sendbf[6] = 176; break;
                case 2: sendbf[6] = 177; break;
                case 3: sendbf[6] = 178; break;
                default: break;
            }
            crc = crc + sendbf[6];
            byte[] temp_i = BitConverter.GetBytes(send_value);
            sendbf[7] = temp_i[0]; crc = crc + sendbf[7];
            sendbf[8] = temp_i[1]; crc = crc + sendbf[8];
            sendbf[9] = temp_i[2]; crc = crc + sendbf[9];
            sendbf[10] = temp_i[3]; crc = crc + sendbf[10];
            sendbf[11] = (byte)(0 - crc);
        }

        public void Monitor_Run(byte[] sendbf, bool brun)
        {
            int crc = 0;
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[4] = 5;
            sendbf[5] = 2;
            sendbf[6] = 177;
            if (brun)
            {
                sendbf[7] = 1;
            }
            else
            {
                sendbf[7] = 0;
            }
            sendbf[8] = 0;
            crc = sendbf[4] + sendbf[5] + sendbf[6] + sendbf[7] + sendbf[8];
            crc = ~crc + 1;
            sendbf[9] = (byte)crc;
        }

        public void MakeCommand(byte[] sendbf, int sn, byte command, float data)
        {

            int crc;
            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            if (sn > 43)  //调试参数 + 修正系数 + 运行模式命令
            {
                if (command == 10)
                {
                    sendbf[4] = 3; crc = sendbf[4];
                    sendbf[5] = (byte)sn; crc = crc + sendbf[5];
                    sendbf[6] = command; crc = crc + sendbf[6];
                    sendbf[7] = (byte)(0 - crc);
                }
                else
                {
                    sendbf[4] = 7; crc = sendbf[4];
                    sendbf[5] = (byte)sn; crc = crc + sendbf[5];
                    sendbf[6] = command; crc = crc + sendbf[6];

                    //sendbf[7]= (byte)data; crc= crc + sendbf[7];
                    //sendbf[8]= (byte)(data>>8); crc= crc + sendbf[8];

                    byte[] temp_i = BitConverter.GetBytes(data);
                    sendbf[7] = temp_i[0]; crc = crc + sendbf[7];
                    sendbf[8] = temp_i[1]; crc = crc + sendbf[8];
                    sendbf[9] = temp_i[2]; crc = crc + sendbf[9];
                    sendbf[10] = temp_i[3]; crc = crc + sendbf[10];

                    sendbf[11] = (byte)(0 - crc);
                }
            }
            else  //运行参数if (sn > -1)
            {
                sendbf[4] = 3; crc = sendbf[4];
                sendbf[5] = (byte)sn; crc = crc + sendbf[5];
                sendbf[6] = command; crc = crc + sendbf[6];
                sendbf[7] = (byte)(0 - crc);
            }
        }

        private void RceCommand(byte[] sendbf, int start, DataR[] data)
        {
            int crc;

            sendbf[0] = 0xfe;
            sendbf[1] = 0xfe;
            sendbf[2] = 0xfe;
            sendbf[3] = 0xfe;
            sendbf[5] = revbuffer[(start + 5) % 256]; crc = sendbf[5];      //序号
            sendbf[6] = revbuffer[(start + 6) % 256]; crc = crc + sendbf[6];  //命令码
            sendbf[7] = 1; crc = crc + sendbf[7];  //确认码
            if (revbuffer[(start + 4) % 256] == 3)
            {

                sendbf[4] = 6; crc = crc + sendbf[4];  //包长
                sendbf[8] = (byte)(data[(start + 5) % 256].SN); crc = crc + sendbf[8];
                sendbf[9] = 0; crc = crc + sendbf[9];
                sendbf[10] = (byte)(0 - crc);
            }
            else if (revbuffer[(start + 4) % 256] == 5)
            {
                sendbf[4] = 4;
                crc = crc + sendbf[4];  //包长
                sendbf[8] = (byte)(0 - crc);
            }
        }

        public int monitor_check(byte[] buffer)
        {
            int i = 0, j = 0, k = 0;
            Boolean found = false;

            for (i = 0; i < 4; i++)
            {
                if (buffer[i] == 0xFE)
                    found = true;
                else
                    found = false;
            }

            if(found)
            {
                if (buffer[5] == 200 && buffer[6] == 0xff && buffer[44] == 0xff)
                {
                    if(buffer[7]==1|| buffer[7]==2)
                    {
                        return 2;
                    }
                }
                else
                {
                    j = buffer[4] + 5;//理论长度
                    k = 0;
                    for (i = 0; i <= buffer[4]; i++)
                    {
                        k = buffer[4 + i] + k;
                    }
                    k = (byte)(k);

                    if (k == 0)//校验通过
                    {
                        if (j == 13)//长度准确
                        {
                            return 1;
                        }
                    }
                }
            }
            return 3;

            
        }

        public DataR Monitor_Solve(byte[] buffer)
        {
            DataR data = new DataR();
            data.SN = buffer[5];
            data.VALUE = BitConverter.ToSingle(buffer, 8);

            return data;
        }


        // 新增：实现IProtocol接口的MonitorRun（适配统一调用）
        public int MonitorRun(byte[] sendbf, bool brun, int addr = 0)
        {
            // 直接调用原有方法
            this.Monitor_Run(sendbf, brun);
            // 返回FE协议的发送长度
            return sendbf[4] + 5;
        }

        // 新增：实现IProtocol接口的MonitorSet（适配统一调用）
        public int MonitorSet(byte[] sendbf, int tempsn, object value = null, object regtype = null, int regOccupyCount = 1)
        {
            byte command = 06;
            // 直接调用原有方法
            this.Monitor_Set(sendbf, (byte)tempsn, command, (float)value);
            // 返回FE协议的发送长度
            return sendbf[4] + 5;
        }

        public int MonitorGet(byte[] sendbf, int tempsn,object num=null, object regtype = null)
        {
            byte index = (byte)(int)tempsn;
            byte command = 03;


            // 直接调用原有方法
            this.Monitor_Get(sendbf, index, command);
            return sendbf[4] + 5;
        }


        public int MonitorCheck(byte[] buffer, object len = null)
        {
            int CheckResult=0;
            CheckResult=this.monitor_check(buffer);
            return CheckResult;
        }

        public DataR MonitorSolve(byte[] buffer, object Readpos = null)
        {
            DataR data = new DataR();
            data = this.Monitor_Solve(buffer);
            return data;
        }

    }
}
