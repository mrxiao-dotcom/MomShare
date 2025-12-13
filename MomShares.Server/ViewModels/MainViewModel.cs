using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;

namespace MomShares.Server.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private Process? _apiProcess;
    private string _configFilePath = "appsettings.json";
    private AppConfig? _config;

    [ObservableProperty]
    private bool _isServiceRunning = false;

    [ObservableProperty]
    private string _serviceStatus = "已停止";

    [ObservableProperty]
    private string _apiPort = "5000";

    [ObservableProperty]
    private string _backupPath = "D:\\Backups\\MomShares";

    [ObservableProperty]
    private string _logText = string.Empty;

    public MainViewModel()
    {
        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                _config = JsonSerializer.Deserialize<AppConfig>(json);
                
                if (_config != null)
                {
                    ApiPort = _config.ApiSettings?.Port ?? "5000";
                    BackupPath = _config.BackupSettings?.BackupPath ?? "D:\\Backups\\MomShares";
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"加载配置失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    [RelayCommand]
    private async Task StartServiceAsync()
    {
        try
        {
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                AddLog("服务已在运行中");
                return;
            }

            // 查找API项目路径
            var currentDir = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(currentDir, "..", "..", "..", "..", "MomShares.Api", "bin", "Debug", "net8.0", "MomShares.Api.exe");
            apiPath = Path.GetFullPath(apiPath);
            
            if (!File.Exists(apiPath))
            {
                // 尝试其他路径
                apiPath = Path.Combine(currentDir, "MomShares.Api.exe");
                if (!File.Exists(apiPath))
                {
                    AddLog($"错误：找不到API程序文件");
                    MessageBox.Show($"找不到API程序文件\n请先编译MomShares.Api项目", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = apiPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"--urls http://localhost:{ApiPort}"
            };

            _apiProcess = Process.Start(startInfo);
            if (_apiProcess != null)
            {
                _apiProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Application.Current.Dispatcher.Invoke(() => AddLog(e.Data));
                    }
                };
                _apiProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Application.Current.Dispatcher.Invoke(() => AddLog($"错误：{e.Data}"));
                    }
                };

                _apiProcess.BeginOutputReadLine();
                _apiProcess.BeginErrorReadLine();

                IsServiceRunning = true;
                ServiceStatus = "运行中";
                AddLog($"服务已启动，端口：{ApiPort}");
                AddLog($"API地址：http://localhost:{ApiPort}");
            }
        }
        catch (Exception ex)
        {
            AddLog($"启动服务失败：{ex.Message}");
            MessageBox.Show($"启动服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    [RelayCommand]
    private void StopService()
    {
        try
        {
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                _apiProcess.Kill();
                _apiProcess.WaitForExit(5000);
                _apiProcess.Dispose();
                _apiProcess = null;

                IsServiceRunning = false;
                ServiceStatus = "已停止";
                AddLog("服务已停止");
            }
            else
            {
                AddLog("服务未运行");
            }
        }
        catch (Exception ex)
        {
            AddLog($"停止服务失败：{ex.Message}");
            MessageBox.Show($"停止服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _config ??= new AppConfig();
            _config.ApiSettings ??= new ApiSettings();
            _config.BackupSettings ??= new BackupSettings();

            _config.ApiSettings.Port = ApiPort;
            _config.BackupSettings.BackupPath = BackupPath;

            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);

            AddLog("配置已保存");
            MessageBox.Show("配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AddLog($"保存配置失败：{ex.Message}");
            MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 检查服务状态
    /// </summary>
    private void CheckServiceStatus()
    {
        // 检查端口是否被占用
        // 这里简化处理，实际可以检查端口
        IsServiceRunning = false;
        ServiceStatus = "已停止";
    }

    /// <summary>
    /// 添加日志
    /// </summary>
    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LogText += $"[{timestamp}] {message}\n";
    }
}

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    public ApiSettings? ApiSettings { get; set; }
    public BackupSettings? BackupSettings { get; set; }
}

/// <summary>
/// API设置
/// </summary>
public class ApiSettings
{
    public string Port { get; set; } = "5000";
}

/// <summary>
/// 备份设置
/// </summary>
public class BackupSettings
{
    public string BackupPath { get; set; } = "D:\\Backups\\MomShares";
    public bool AutoBackupEnabled { get; set; } = true;
    public string AutoBackupTime { get; set; } = "02:00:00";
}
