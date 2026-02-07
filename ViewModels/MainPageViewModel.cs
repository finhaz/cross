using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using cross.Communication;
using cross.Mvvm;

namespace cross.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        // 绑定到全局AppViewModel
        private AppViewModel _appViewModel = AppViewModel.Instance;
        public AppViewModel AppViewModel => _appViewModel;

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
    }
}
