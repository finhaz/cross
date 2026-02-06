# ocean
## 功能
串口上位机
## 编写方式
基于Avalonia制作
## 说明
该工程是ocean工程的Avalonia版本
对winform的fruit不再更新
对WPF的Ocean则很可能也不再更新，其功能将会迁移进入Avalonia工程，以实现跨平台
不定期会迁移各个界面内的组件为mvvm模式
目前支持串口、以太网的通讯，以及部分TTL转CAN模块
实现Modbus RTU/TCP协议，CAN的DBC报文解析
目前主要考虑上位机的通讯，因此暂时不考虑支持其他通讯