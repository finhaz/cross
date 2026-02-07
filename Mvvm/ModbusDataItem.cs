using cross.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.ViewModels
{
    // 你的命名空间
    public class ModbusDataItem : ObservableObject
    {
        private int _id;
        private string _name;
        private object _value; // 核心：用object存储int/double
        private object _rvalue; // 核心：用object存储int/double
        private double _command;
        private bool _isButtonClicked;
        private string _unit;
        private string _rangle;
        private string _selectedOption;
        private int _addr;
        private int _number;
        private int _nOffSet;
        private int _nBit;
        private float _coefficient;
        private int _offset;
        private int _decimalPlaces;
        private string _transferType;
        private string _displayType; // 浮点数/整形数
        private string _byteOrder;
        private string _wordOrder;
        private bool _isDrawCurve;
        private int _intervalTime;

        // 所有属性按原DataTable列名定义，保持Binding兼容
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public object Value // 核心属性：存储int或double
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public object RValue // 核心属性：存储int或double
        {
            get => _rvalue;
            set => SetProperty(ref _rvalue, value);
        }

        public double Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        public bool IsButtonClicked
        {
            get => _isButtonClicked;
            set => SetProperty(ref _isButtonClicked, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public string Rangle
        {
            get => _rangle;
            set => SetProperty(ref _rangle, value);
        }

        public string SelectedOption
        {
            get => _selectedOption;
            //set => SetProperty(ref _selectedOption, value);
            set
            {
                // 核心逻辑：_selectedOption，调整状态
                if (SetProperty(ref _selectedOption, value))
                {
                    
                    ConvertValueBySelectedOption();
                }
            }
        }

        public int Addr
        {
            get => _addr;
            set => SetProperty(ref _addr, value);
        }

        public int Number
        {
            get => _number;
            set
            {
                if (SetProperty(ref _number, value))
                {
                    ConvertValueByNumber();
                }
            }       
        }

        public int NOffSet
        {
            get => _nOffSet;
            set => SetProperty(ref _nOffSet, value);
        }

        public int NBit
        {
            get => _nBit;
            set 
            {
                if (SetProperty(ref _nBit, value))
                {
                    // 触发属性变更通知（ObservableObject的核心方法）
                    OnPropertyChanged();
                    ConvertValueByNBit();
                } 
            }
        }

        public float Coefficient
        {
            get => _coefficient;
            //set => SetProperty(ref _coefficient, value);
            set
            {
                // 核心逻辑：切换_coefficient时，自动转换Value值
                if (SetProperty(ref _coefficient, value))
                {
                    ConvertValueByCoefficient();
                }
            }
        }

        public int Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set => SetProperty(ref _decimalPlaces, value);
        }

        public string TransferType
        {
            get => _transferType;
            set => SetProperty(ref _transferType, value);
        }

        public string DisplayType
        {
            get => _displayType;
            set
            {
                // 核心逻辑：切换DisplayType时，自动转换Value的类型
                if (SetProperty(ref _displayType, value))
                {
                    ConvertValueByDisplayType();
                }
            }
        }

        public string ByteOrder
        {
            get => _byteOrder;
            set => SetProperty(ref _byteOrder, value);
        }

        public string WordOrder
        {
            get => _wordOrder;
            set => SetProperty(ref _wordOrder, value);
        }

        public bool IsDrawCurve
        {
            get => _isDrawCurve;
            set => SetProperty(ref _isDrawCurve, value);
        }

        public int IntervalTime
        {
            get => _intervalTime;
            set => SetProperty(ref _intervalTime, value);
        }

        /// <summary>
        /// 根据DisplayType自动转换Value的类型（仅当前行生效）
        /// </summary>
        private void ConvertValueByDisplayType()
        {
            if (Value == null)
            {
                Value = DisplayType == "十进制整数" ? 0 : 0.0;
                return;
            }

            try
            {
                switch (DisplayType)
                {
                    case "十进制整数":
                        // 浮点数转十进制整数（四舍五入）
                        double decimalDouble = Convert.ToDouble(Value);
                        Value = Convert.ToInt32(Math.Round(decimalDouble));
                        break;

                    case "十六进制整数":
                        // 浮点数转整数（作为十六进制的存储值，显示时转格式）
                        double hexDouble = Convert.ToDouble(Value);
                        Value = Convert.ToInt32(Math.Round(hexDouble)); // 存储为int
                        break;

                    case "浮点数":
                    default:
                        // 整数（十进制/十六进制）转浮点数
                        int floatInt = Convert.ToInt32(Value);
                        Value = Convert.ToDouble(floatInt);
                        break;
                }
            }
            catch (Exception)
            {
                // 转换失败时设默认值
                Value = DisplayType == "浮点数" ? 0.0 : 0;
            }
        }

        private void ConvertValueByCoefficient()
        {
            // 先判断是否为数值类型
            if (RValue is int || RValue is float || RValue is double || RValue is decimal)
            {
                float rValueFloat = Convert.ToSingle(RValue);
                Value = rValueFloat * Coefficient;
                //Console.WriteLine($"计算结果：{Value}");
            }

        }

        private void ConvertValueByNBit()
        {
            if (NBit > Number * 16)
                NBit = Number * 16;
            if (SelectedOption == "线圈状态(RW)" || SelectedOption == "离散输入(RO)")
            {
                Number = 1;
                NBit = 1;
            }
        }

        private void ConvertValueByNumber()
        {
            if (SelectedOption == "线圈状态(RW)" || SelectedOption == "离散输入(RO)")
            {
                Number = 1;
                NBit = 1;
            }
            else
                NBit = Number * 16;
        }

        private void ConvertValueBySelectedOption()
        {
            if (Value == null)
            {
                Value = DisplayType == "十进制整数" ? 0 : 0.0;
                return;
            }

            try
            {
                switch (SelectedOption)
                {
                    case "线圈状态(RW)":
                    case "离散输入(RO)":
                        Number = 1;
                        DisplayType = "十进制整数";
                        NBit = 1;
                        TransferType = "位数据";
                        break;
                    case "保持寄存器(RW)":
                    case "输入寄存器(RO)":
                        Number = 1;
                        DisplayType = "浮点数";
                        NBit = 16;
                        TransferType = "无符号整数";
                        break;
                    default:
                        break;

                }
            }
            catch (Exception)
            {
                // 转换失败时设默认值
                Value = DisplayType == "浮点数" ? 0.0 : 0;
            }
        }
    }
}
