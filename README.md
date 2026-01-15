# CP2112 SMBus Device Wrapper for .NET/WPF

完整的CP2112 SMBus设备C#包装器，支持WPF应用开发。

## 功能特性

- **完整P/Invoke封装**: 对`SLABHIDtoSMBus.dll`的完整封装
- **IDisposable支持**: 自动资源管理，确保设备正确关闭
- **异步操作**: 所有I/O操作都支持async/await
- **事件驱动**: C回调转换为C#事件
- **线程安全**: 内置锁机制，支持多线程调用
- **状态机管理**: 完整的设备状态跟踪
- **错误处理**: 自动重试机制和详细错误信息
- **多设备支持**: DeviceManager支持管理多个CP2112实例
- **WPF集成**: 提供ViewModel示例，便于数据绑定

## 项目结构

```
convert_C_to_CS/
├── Cp2112Device/              # 主库项目
│   ├── Native/
│   │   └── NativeMethods.cs   # P/Invoke声明
│   ├── Models/
│   │   └── DeviceState.cs     # 状态、配置模型
│   ├── Exceptions/
│   │   └── Cp2112Exception.cs # 自定义异常
│   ├── Events/
│   │   └── DeviceEventArgs.cs # 事件参数
│   ├── Cp2112Device.cs        # 主设备控制器
│   └── Cp2112DeviceManager.cs # 设备管理器
├── Examples/
│   └── ExampleUsage.cs        # 使用示例
└── demo_to_convert/
    └── src/                   # 原始C代码和DLL
```

## 快速开始

### 1. 基本使用

```csharp
using Cp2112Device;
using Cp2112Device.Models;

// 创建设备实例
using var device = new Cp2112Device();

// 打开设备
await device.OpenAsync();

// 配置设备
await device.ConfigureAsync(SmbusConfig.Default);

// 读取数据
var data = await device.ReadAsync(0xC8, 2, 0x8D);

// 写入数据
var writeData = new byte[] { 0xEA, 0x00, 0x00 };
await device.WriteAsync(0xC8, writeData);

// 读取16位值
ushort value = await device.ReadUInt16Async(0xC8, 0x8D);
```

### 2. WPF ViewModel集成

```csharp
public class MainViewModel
{
    private readonly Cp2112Device _device = new();

    public async Task ConnectAsync()
    {
        await _device.OpenAsync();
        await _device.ConfigureAsync(SmbusConfig.Default);
    }

    public async Task<float> ReadTemperatureAsync()
    {
        var raw = await _device.ReadInt16Async(0xC8, 0x8D);
        return raw / 32.0f - 40.0f;
    }
}
```

### 3. 多设备管理

```csharp
using var manager = new Cp2112DeviceManager();

// 扫描设备
var devices = await manager.ScanDevicesAsync();

// 打开所有设备
await manager.OpenAllDevicesAsync(SmbusConfig.Default);

// 获取特定设备
var device = manager.GetDeviceBySerial("123456");
```

## API 参考

### Cp2112Device类

#### 方法
| 方法 | 描述 |
|------|------|
| `OpenAsync()` | 打开设备 |
| `CloseAsync()` | 关闭设备 |
| `ConfigureAsync(config)` | 配置SMBus参数 |
| `ReadAsync(addr, len, targetAddr)` | 读取数据 |
| `WriteAsync(addr, buffer)` | 写入数据 |
| `ReadUInt16Async(addr, reg)` | 读取16位无符号整数 |
| `ReadInt16Async(addr, reg)` | 读取16位有符号整数 |

#### 事件
| 事件 | 描述 |
|------|------|
| `StateChanged` | 设备状态改变 |
| `DataReceived` | 数据接收 |
| `DataSent` | 数据发送 |
| `ErrorOccurred` | 错误发生 |
| `TransferStatusChanged` | 传输状态改变 |

### SmbusConfig类

```csharp
public class SmbusConfig
{
    public uint BitRate { get; set; } = 100000;      // 比特率
    public byte AckAddress { get; set; } = 0x02;     // ACK地址
    public bool AutoReadRespond { get; set; }        // 自动读响应
    public ushort WriteTimeout { get; set; } = 10;   // 写超时(ms)
    public ushort ReadTimeout { get; set; } = 10;    // 读超时(ms)
    public bool SclLowTimeout { get; set; } = true;  // SCL低超时
    public ushort TransferRetries { get; set; } = 0; // 传输重试次数
    public uint ResponseTimeout { get; set; } = 100; // 响应超时(ms)
}
```

### 预设配置

- `SmbusConfig.Default` - 100kHz标准速度
- `SmbusConfig.HighSpeed` - 400kHz高速
- `SmbusConfig.LowSpeed` - 10kHz低速（更可靠）

## 线程安全

所有公共方法都是线程安全的：
- 使用`SemaphoreSlim`确保操作互斥
- 支持取消令牌（CancellationToken）
- 自动重试机制（最多3次）

## 错误处理

```csharp
try
{
    await device.OpenAsync();
}
catch (DeviceNotFoundException)
{
    // 设备未找到
}
catch (ReadTimeoutException)
{
    // 读取超时
}
catch (Cp2112Exception ex)
{
    // 其他错误 (ex.ErrorCode包含错误码)
}
```

## 构建项目

要求：.NET 8.0 SDK

```bash
# 构建主库
dotnet build Cp2112Device/Cp2112Device.csproj

# 构建示例
dotnet build Examples/Examples.csproj

# 运行示例
dotnet run --project Examples/Examples.csproj
```

## DLL依赖

确保以下DLL与可执行文件在同一目录：
- `SLABHIDDevice.dll`
- `SLABHIDtoSMBus.dll`

项目会自动从`demo_to_convert/src/`复制这些文件。

## WPF集成示例

### XAML
```xml
<StackPanel>
    <TextBlock Text="{Binding Status}" />
    <TextBlock Text="{Binding Temperature, StringFormat='Temperature: {0:F2}°C'}" />
    <Button Content="Connect" Command="{Binding ConnectCommand}" />
    <Button Content="Read" Command="{Binding ReadCommand}" />
</StackPanel>
```

### ViewModel
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private readonly Cp2112Device _device = new();

    private float _temperature;
    public float Temperature
    {
        get => _temperature;
        set { _temperature = value; OnPropertyChanged(); }
    }

    public async Task ReadAsync()
    {
        var raw = await _device.ReadInt16Async(0xC8, 0x8D);
        Temperature = raw / 32.0f - 40.0f;
    }
}
```

## 许可证

本项目基于原始C代码重构，请遵守Silicon Labs的SDK许可协议。

## 更新日志

### v1.0.0
- 初始版本
- 完整的P/Invoke封装
- 异步操作支持
- 事件系统
- 多设备管理
- WPF ViewModel示例
