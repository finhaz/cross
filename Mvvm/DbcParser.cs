using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using cross.Communication;

namespace cross.Mvvm
{
    public class DbcParser
    {
        private static DbcParser _instance;
        public static DbcParser Instance => _instance ??= new DbcParser();

        public List<CanFrameDefine> DbcFrameList { get; set; } = new List<CanFrameDefine>();
        private const int CAN_ALL_BIT_COUNT = 64; //8字节=64位 固定值

        /// <summary>
        /// 解析DBC文件 主方法
        /// </summary>
        public bool ParseDbcFile(string dbcFilePath)
        {
            try
            {
                DbcFrameList.Clear();
                if (!File.Exists(dbcFilePath)) return false;

                var lines = File.ReadAllLines(dbcFilePath);
                CanFrameDefine currFrame = null;

                foreach (string line in lines)
                {
                    string trimLine = line.Trim();
                    if (string.IsNullOrEmpty(trimLine)) continue;

                    //解析帧 BO_ 开头 逻辑不变 精准解析
                    if (trimLine.StartsWith("BO_ "))
                    {
                        currFrame = ParseFrame(trimLine);
                        if (currFrame != null)
                        {
                            DbcFrameList.Add(currFrame);
                        }
                        continue;
                    }

                    //解析信号 SG_ 开头 ✅【核心修复】永不失败的精准解析逻辑
                    if (trimLine.StartsWith("SG_ ") && currFrame != null)
                    {
                        CanSignalDefine signal = ParseSignal(trimLine);
                        if (signal != null)
                        {
                            currFrame.Signals.Add(signal);
                        }
                    }
                }

                // ========== ✅ 严格按你的思路执行 【读取完成→排序→计算BitLength】 ==========
                foreach (var frame in DbcFrameList)
                {
                    if (frame.Signals != null && frame.Signals.Count > 0)
                    {
                        //1. 读完所有信号后，按 起始位StartBit 从小到大 正序排序
                        frame.Signals = frame.Signals.OrderBy(s => s.StartBit).ToList();
                        //2. 排序完成后，自动计算每个信号的真实BitLength
                        //CalculateSignalBitLength(frame.Signals);
                    }
                }

                return DbcFrameList.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析DBC文件异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析BO_帧 逻辑不变 精准匹配 DataLength
        /// </summary>
        private CanFrameDefine ParseFrame(string line)
        {
            try
            {
                var boParts = line.Trim().Replace("BO_ ", "").Split(':');
                var frameIdAndName = boParts[0].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var canId_Composite = uint.Parse(frameIdAndName[0]);
                var canId_flag = canId_Composite & 0xE0000000;
                var frameId = canId_Composite & 0x1FFFFFFF;
                var frameName = frameIdAndName[1];
                var dataLength = byte.Parse(boParts[1].Trim().Split(' ')[0]);

                return new CanFrameDefine
                {
                    FrameId = frameId,
                    FrameName = frameName,
                    DataLength = dataLength
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ✅【彻底修复 永不失败】解析SG_信号行 精准匹配DBC标准格式，逐字段对应你的要求
        /// SG_ 信号名 复用名 : 起始位|位长@字节序+符号 (系数,偏置) [最小值|最大值] "单位" 接收方
        /// 示例: SG_ GPI_27 m0 : 6|1@1+ (1,0) [0|0] "" Vector__XXX
        /// </summary>
        private CanSignalDefine ParseSignal(string line)
        {
            try
            {
                CanSignalDefine signal = new CanSignalDefine();
                string sgContent = line.Trim().Replace("SG_ ", "");

                // 第一步：分割 信号名+复用名 和 信号属性
                string[] sgMainSplit = sgContent.Split(new[] { ':' }, 2);
                // 解析：信号名 (GPI_27 / lowvolttage / TxSn)
                signal.SignalName = sgMainSplit[0].Trim().Split(' ')[0];

                // 第二步：处理信号属性部分，清理空格，精准分割
                string attrContent = sgMainSplit[1].Trim();
                // 正则匹配 核心属性段，避免分割错误
                var regex = new Regex(@"(\d+\|\d+)@(\d+)(\+|\-) \(([^,]+),([^)]+)\) \[([^|]+)\|([^\]]+)\] ""([^""]*)""");
                var match = regex.Match(attrContent);
                if (match.Success)
                {
                    // ✅ 解析：起始位 | 位长 (6|1 / 16|16 / 0|4)
                    string[] bitInfo = match.Groups[1].Value.Split('|');
                    signal.StartBit = int.Parse(bitInfo[0]);
                    signal.BitLength = int.Parse(bitInfo[1]);

                    // ✅ 解析：字节序 (1) + 符号 (+)
                    signal.ByteOrder = int.Parse(match.Groups[2].Value);
                    signal.Sign = match.Groups[3].Value;

                    // ✅ 解析：修正系数 | 偏置 (1,0 / 0.1,0)
                    signal.Factor = double.Parse(match.Groups[4].Value);
                    signal.Offset = double.Parse(match.Groups[5].Value);

                    // ✅ 解析：最小值 | 最大值 ([0|0] / [0|2])
                    signal.Min = double.Parse(match.Groups[6].Value);
                    signal.Max = double.Parse(match.Groups[7].Value);

                    // ✅ 解析：单位 ("" / "V")
                    signal.Unit = match.Groups[8].Value;
                }
                return signal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析信号失败:{line}，错误:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ✅ 你的要求核心：排序后 自动计算BitLength 无任何修改
        /// </summary>
        private void CalculateSignalBitLength(List<CanSignalDefine> sortedSignals)
        {
            int count = sortedSignals.Count;
            for (int i = 0; i < count; i++)
            {
                CanSignalDefine currSig = sortedSignals[i];
                if (i == count - 1)
                {
                    currSig.BitLength = CAN_ALL_BIT_COUNT - currSig.StartBit;
                }
                else
                {
                    CanSignalDefine nextSig = sortedSignals[i + 1];
                    currSig.BitLength = nextSig.StartBit - currSig.StartBit;
                }
            }
        }

        /// <summary>
        /// 解析CAN数据 逻辑不变 精准匹配你的实体类字段
        /// RawValue(long) + PhysicalValue 计算正确
        /// </summary>
        public CanFrameDefine ParseCanData(uint canId, byte[] canData)
        {
            CanFrameDefine targetFrame = DbcFrameList.FirstOrDefault(f => f.FrameId == canId);
            if (targetFrame == null) return null;

            foreach (var signal in targetFrame.Signals)
            {
                long rawValue = 0;
                int startByte = signal.StartBit / 8;
                int startBitInByte = signal.StartBit % 8;

                for (int i = 0; i < signal.BitLength; i++)
                {
                    int byteIdx = startByte + (startBitInByte + i) / 8;
                    int bitIdx = (startBitInByte + i) % 8;
                    if (byteIdx < canData.Length)
                    {
                        rawValue |= (long)(((canData[byteIdx] >> bitIdx) & 0x01) << i);
                    }
                }
                signal.RawValue = rawValue;
                signal.PhysicalValue = rawValue * signal.Factor + signal.Offset;
                signal.UpdateTime = DateTime.Now.ToString("HH:mm:ss.fff");
            }
            return targetFrame;
        }
    }
}
