using cross.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO.Ports;
using cross.Mvvm;

namespace cross.Communication
{
    /// <summary>
    /// 串口配置类：集中封装所有串口相关配置属性
    /// </summary>
    public class SerialPortConfig : ObservableObject
    {
        #region 端口名相关
        private ObservableCollection<string> _portNames = new ObservableCollection<string>();
        public ObservableCollection<string> PortNames
        {
            get => _portNames;
            set => SetProperty(ref _portNames, value);
        }

        private string _selectedPortName = string.Empty;
        public string SelectedPortName
        {
            get => _selectedPortName;
            set => SetProperty(ref _selectedPortName, value);
        }
        #endregion

        #region 波特率相关
        private ObservableCollection<int> _baudRates = new ObservableCollection<int> { 4800, 9600, 19200, 38400, 43000, 56000, 115200 };
        public ObservableCollection<int> BaudRates
        {
            get => _baudRates;
            set => SetProperty(ref _baudRates, value);
        }

        private int _selectedBaudRate = 9600;
        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }
        #endregion

        #region 校验位相关
        private ObservableCollection<string> _parityNames = new ObservableCollection<string> { "无", "奇校验", "偶校验" };
        public ObservableCollection<string> ParityNames
        {
            get => _parityNames;
            set => SetProperty(ref _parityNames, value);
        }

        private string _selectedParityName = "无";
        public string SelectedParityName
        {
            get => _selectedParityName;
            set
            {
                if (SetProperty(ref _selectedParityName, value))
                {
                    // 同步更新Parity枚举：联动逻辑封装在配置类内部
                    SelectedParity = value switch
                    {
                        "无" => Parity.None,
                        "奇校验" => Parity.Odd,
                        "偶校验" => Parity.Even,
                        _ => Parity.None
                    };
                }
            }
        }

        private Parity _selectedParity = Parity.None;
        public Parity SelectedParity
        {
            get => _selectedParity;
            set => SetProperty(ref _selectedParity, value);
        }
        #endregion

        #region 数据位相关
        private ObservableCollection<int> _dataBits = new ObservableCollection<int> { 6, 7, 8 };
        public ObservableCollection<int> DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        private int _selectedDataBit = 8;
        public int SelectedDataBit
        {
            get => _selectedDataBit;
            set => SetProperty(ref _selectedDataBit, value);
        }
        #endregion

        #region 停止位相关
        /// <summary>
        /// 停止位可选值集合（命名为StopBitValues，避免与StopBits枚举冲突）
        /// </summary>
        private ObservableCollection<int> _stopBitValues = new ObservableCollection<int> { 1, 2 };
        public ObservableCollection<int> StopBitValues
        {
            get => _stopBitValues;
            set => SetProperty(ref _stopBitValues, value);
        }

        /// <summary>
        /// 选中的停止位数值（1/2）
        /// </summary>
        private int _selectedStopBit = 1;
        public int SelectedStopBit
        {
            get => _selectedStopBit;
            set
            {
                if (SetProperty(ref _selectedStopBit, value))
                {
                    // 同步更新StopBits枚举：联动逻辑封装在配置类内部
                    SelectedStopBitEnum = value == 1 ? StopBits.One : StopBits.Two;
                }
            }
        }

        /// <summary>
        /// 选中的停止位枚举（SerialPort的StopBits类型）
        /// </summary>
        private StopBits _selectedStopBitEnum = StopBits.One;
        public StopBits SelectedStopBitEnum
        {
            get => _selectedStopBitEnum;
            set => SetProperty(ref _selectedStopBitEnum, value);
        }
        #endregion
    }
}
