using cross.database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Interfaces
{
    /// <summary>
    /// 协议统一接口（包含所有需要统一的操作）
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// 执行监控运行（原Monitor_Run）
        /// </summary>
        /// <param name="sendbf">发送字节数组</param>
        /// <param name="brun">运行标识</param>
        /// <param name="addr">地址（部分协议可选）</param>
        /// <returns>发送长度</returns>
        int MonitorRun(byte[] sendbf, bool brun, int addr = 0);

        /// <summary>
        /// 执行监控设置（原Monitor_Set/Monitor_Set_06）
        /// </summary>
        /// <param name="sendbf">发送字节数组</param>
        /// <param name="tempsn">临时序号</param>
        /// <param name="data">数据对象（FE协议专用，可选）</param>
        /// <param name="value">设置值</param>
        /// <returns>发送长度</returns>
        int MonitorSet(byte[] sendbf, int tempsn, object value = null,object regtype = null, int regOccupyCount = 1);

        /// </summary>
        /// <param name="sendbf">发送字节数组</param>
        /// <param name="tempsn">临时序号</param>
        /// <param name="data">数据对象（FE协议专用，可选）</param>
        /// <param name="num">总长度（Modbus协议需要）</param>
        /// <returns>发送长度</returns>
        int MonitorGet(byte[] sendbf, int tempsn, object num = null, object regtype = null);

        /// </summary>
        /// <param name="buffer">接收字节数组</param>
        /// <param name="len">总长度（Modbus协议需要）</param>
        int MonitorCheck(byte[] buffer, object len = null);


        DataR MonitorSolve(byte[] buffer,  object Readpos=null);
    }
}
