using cross.Communication;
using cross.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace cross.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        // 绑定到全局AppViewModel
        private AppViewModel _appViewModel = AppViewModel.Instance;
        public AppViewModel AppViewModel => _appViewModel;

        public ICommand ComboBoxSelectCommand { get; }
        // 通讯类型选项（供ComboBox绑定）
        public List<KeyValuePair<string, CommunicationType>> CommTypeOptions { get; } = new()
        {
            new KeyValuePair<string, CommunicationType>("串口通讯", CommunicationType.SerialPort),
            new KeyValuePair<string, CommunicationType>("网络通讯", CommunicationType.Ethernet),
            new KeyValuePair<string, CommunicationType>("CAN通讯", CommunicationType.CAN)
        };

        // 当前选中的通讯类型（双向绑定到ComboBox）
        public CommunicationType SelectedCommType
        {
            get => _appViewModel.SelectedCommType;
            set => _appViewModel.SelectedCommType = value;
        }

        public MainPageViewModel()
        {
            // 原有命令初始化保留
            ComboBoxSelectCommand = new RelayCommand<object>(ExecuteSelect);
        }

        private void ExecuteSelect(object _)
        {
            switch (SelectedCommType)
            {
                case CommunicationType.SerialPort:
                    // 选择串口：创建串口实例（仅创建，不打开/不绑定事件）
                    CommunicationManager.Instance.CreateSerialInstance();
                    break;
                case CommunicationType.CAN:
                    // 针对TTL转CAN，创建串口实例（仅创建，不打开/不绑定事件）
                    CommunicationManager.Instance.CreateSerialInstance();
                    break;
                case CommunicationType.Ethernet:
                    // 仅创建网络通讯空实例，不指定TCP/UDP
                    CommunicationManager.Instance.CreateEthernetInstance();
                    break;
                case CommunicationType.IIC:
                    // 预留，暂不处理
                    break;
            }
        }

    }
}
