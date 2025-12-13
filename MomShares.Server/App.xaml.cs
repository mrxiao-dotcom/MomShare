using System.Configuration;
using System.Data;
using System.Windows;
using MomShares.Server.ViewModels;

namespace MomShares.Server;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnExit(ExitEventArgs e)
    {
        // 确保在应用关闭时停止Web服务器
        try
        {
            if (MainWindow?.DataContext is MainViewModel viewModel)
            {
                if (viewModel.IsServiceRunning)
                {
                    await viewModel.StopServiceAsync();
                }
            }
        }
        catch { }
        base.OnExit(e);
    }
}

