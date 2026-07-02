using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CJ.Plug.LicenseApiClient;

namespace CJ.Plug.Desktop.ViewModels;

public partial class UpgradeViewModel : ObservableObject
{
    private readonly ILicenseApiClient _licenseClient;
    private readonly HttpClient _httpClient;
    private DispatcherTimer? _pollTimer;

    public UpgradeViewModel(ILicenseApiClient licenseClient, HttpClient httpClient)
    {
        _licenseClient = licenseClient;
        _httpClient = httpClient;
    }

    [ObservableProperty]
    private string _orderId = string.Empty;

    [ObservableProperty]
    private BitmapImage? _qrCodeImage;

    [ObservableProperty]
    private bool _isWaiting = true;

    [ObservableProperty]
    private bool _isPaid;

    [ObservableProperty]
    private bool _isActivated;

    [ObservableProperty]
    private string _statusText = "等待扫码支付...";

    [ObservableProperty]
    private string _amountText = "6.00";

    /// <summary>激活成功时触发，由弹窗代码后置订阅以关闭窗口。</summary>
    public event Action? UpgradeCompleted;

    /// <summary>创建订单、加载二维码并开始轮询</summary>
    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        try
        {
            var response = await _licenseClient.CreateUpgradeAsync();
            if (response == null)
            {
                StatusText = "创建订单失败，请重试";
                return;
            }

            OrderId = response.OrderId;
            StatusText = "请扫描二维码支付 6 元";
            IsWaiting = true;
            IsPaid = false;
            IsActivated = false;

            // 优先使用码支付返回的二维码 URL
            if (!string.IsNullOrEmpty(response.QrCodeUrl))
            {
                await LoadQrCodeFromUrlAsync(response.QrCodeUrl);
            }
            else
            {
                // 回退到本地占位图
                LoadQrCodeFromLocal();
            }

            StartPolling();
        }
        catch (Exception ex)
        {
            StatusText = $"创建订单失败：{ex.Message}";
        }
    }

    private async Task LoadQrCodeFromUrlAsync(string url)
    {
        try
        {
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(imageBytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            QrCodeImage = bitmap;
        }
        catch (Exception ex)
        {
            StatusText = $"二维码加载失败：{ex.Message}";
            LoadQrCodeFromLocal();
        }
    }

    private void LoadQrCodeFromLocal()
    {
        var qrPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Resources", "upgrade-qrcode.png");
        if (File.Exists(qrPath))
            QrCodeImage = new BitmapImage(new Uri(qrPath));
    }

    private void StartPolling()
    {
        _pollTimer?.Stop();
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += async (_, _) => await PollStatusAsync();
        _pollTimer.Start();
    }

    private async Task PollStatusAsync()
    {
        if (string.IsNullOrEmpty(OrderId)) return;

        try
        {
            var status = await _licenseClient.GetUpgradeStatusAsync(OrderId);
            if (status == null) return;

            switch (status.Status)
            {
                case "pending":
                    IsWaiting = true;
                    StatusText = "等待扫码支付...";
                    break;

                case "paid":
                    IsWaiting = false;
                    IsPaid = true;
                    StatusText = "支付成功，正在激活...";
                    break;

                case "activated":
                    IsWaiting = false;
                    IsPaid = false;
                    IsActivated = true;
                    StatusText = "激活完成！";
                    _pollTimer?.Stop();
                    UpgradeCompleted?.Invoke();
                    break;

                case "expired":
                    IsWaiting = false;
                    StatusText = "订单已过期，请重新创建";
                    _pollTimer?.Stop();
                    break;
            }
        }
        catch
        {
            // 网络异常时静默重试
        }
    }

    public void StopPolling()
    {
        _pollTimer?.Stop();
    }
}
