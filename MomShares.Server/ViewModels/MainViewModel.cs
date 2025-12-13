using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using MomShares.Api;

namespace MomShares.Server.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private WebApplication? _webApp;
    private Task? _webAppTask;
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
            if (_webApp != null)
            {
                AddLog("服务已在运行中");
                return;
            }

            AddLog("正在启动Web服务器...");
            
            // 查找API项目目录和数据库文件
            var currentDir = Directory.GetCurrentDirectory();
            var apiDir = Path.Combine(currentDir, "..", "..", "..", "..", "MomShares.Api");
            apiDir = Path.GetFullPath(apiDir);
            
            // 查找原有的数据库文件（可能在项目目录下）
            var projectDbPath = Path.Combine(apiDir, "MomShares.db");
            var localAppDataDbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MomShares", "MomShares.db");
            
            // 如果项目目录下有数据库，优先使用
            if (File.Exists(projectDbPath))
            {
                AddLog($"找到项目数据库: {projectDbPath}");
                // 设置环境变量，让WebAppBuilder使用这个数据库
                Environment.SetEnvironmentVariable("MomShares_DbPath", projectDbPath);
            }
            else if (File.Exists(localAppDataDbPath))
            {
                AddLog($"找到本地数据库: {localAppDataDbPath}");
            }
            else
            {
                AddLog($"未找到数据库文件，将创建新数据库");
            }
            
            if (Directory.Exists(apiDir))
            {
                var originalDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(apiDir);
                AddLog($"设置工作目录为: {apiDir}");
                
                try
                {
                    // 创建Web应用
                    var urls = $"http://localhost:{ApiPort}";
                    _webApp = WebAppBuilder.CreateWebApplication(null, urls);
                }
                finally
                {
                    // 恢复原始工作目录
                    Directory.SetCurrentDirectory(originalDir);
                }
            }
            else
            {
                // 如果找不到API目录，使用当前目录
                AddLog($"未找到API项目目录，使用当前目录: {currentDir}");
                var urls = $"http://localhost:{ApiPort}";
                _webApp = WebAppBuilder.CreateWebApplication(null, urls);
            }

            // 初始化数据库
            AddLog("正在初始化数据库...");
            await WebAppBuilder.InitializeDatabaseAsync(_webApp);
            AddLog("数据库初始化完成");

            // 在后台任务中运行Web应用
            _webAppTask = Task.Run(async () =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsServiceRunning = true;
                        ServiceStatus = "运行中";
                        AddLog("========================================");
                        AddLog("  份额管理系统 - Web服务器已启动");
                        AddLog("========================================");
                        AddLog($"  管理界面: http://localhost:{ApiPort}");
                        AddLog($"  API文档: http://localhost:{ApiPort}/swagger");
                        AddLog("========================================");
                    });

                    await _webApp.RunAsync();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddLog($"服务运行错误：{ex.Message}");
                        IsServiceRunning = false;
                        ServiceStatus = "已停止";
                        _webApp = null;
                        _webAppTask = null;
                    });
                }
            });

            // 等待一小段时间确保服务启动
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            AddLog($"启动服务失败：{ex.Message}");
            AddLog($"错误详情：{ex.StackTrace}");
            MessageBox.Show($"启动服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            IsServiceRunning = false;
            ServiceStatus = "已停止";
            _webApp = null;
            _webAppTask = null;
        }
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    [RelayCommand]
    public async Task StopServiceAsync()
    {
        try
        {
            if (_webApp != null)
            {
                AddLog("正在停止服务...");
                
                // 停止Web应用
                await _webApp.StopAsync();
                await _webApp.DisposeAsync();
                _webApp = null;

                // 等待任务完成
                if (_webAppTask != null)
                {
                    try
                    {
                        await Task.WhenAny(_webAppTask, Task.Delay(5000));
                    }
                    catch { }
                    _webAppTask = null;
                }

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
            IsServiceRunning = false;
            ServiceStatus = "已停止";
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
