using Microsoft.Extensions.DependencyInjection;
using SyncClipboard.Core.Clipboard;
using SyncClipboard.Core.Commons;
using SyncClipboard.Core.Interfaces;
using SyncClipboard.Core.Models;
using SyncClipboard.Core.Models.UserConfigs;
using SyncClipboard.Core.Utilities.Image;
using SyncClipboard.Core.Utilities.Notification;
using System.Text.RegularExpressions;

namespace SyncClipboard.Core.UserServices;

public class EasyCopyImageSerivce : ClipboardHander
{
    #region override ClipboardHander

    public override string SERVICE_NAME => "图片轻松拷贝";
    public override string LOG_TAG => "EASY IMAGE";

    protected override ILogger Logger => _logger;
    protected override IContextMenu? ContextMenu => _serviceProvider.GetRequiredService<IContextMenu>();
    protected override IClipboardChangingListener ClipboardChangingListener
                                                  => _serviceProvider.GetRequiredService<IClipboardChangingListener>();
    protected override bool SwitchOn
    {
        get => _clipboardAssistConfig.EasyCopyImageSwitchOn;
        set
        {
            _clipboardAssistConfig.EasyCopyImageSwitchOn = value;
            _configManager.SetConfig(ConfigKey.ClipboardAssist, _clipboardAssistConfig);
        }
    }

    private bool DownloadWebImageEnabled => _clipboardAssistConfig.DownloadWebImage;

    protected override async void HandleClipboard(ClipboardMetaInfomation meta, CancellationToken cancelToken)
    {
        await ProcessClipboard(meta, false, cancelToken);
        //Task[] tasks = {
        //    ProcessClipboard(meta, false, cancelToken),
        //    ProcessClipboard(meta, true, cancelToken)
        //};
        //foreach (var task in tasks)
        //{
        //    task.ContinueWith((_) => this.CancelProcess(), TaskContinuationOptions.OnlyOnRanToCompletion);
        //}
    }

    protected override CancellationToken StopPreviousAndGetNewToken()
    {
        TrayIcon.SetStatusString(SERVICE_NAME, "Running.");
        _progress?.CancelSicent();
        _progress = null;
        return base.StopPreviousAndGetNewToken();
    }

    #endregion override ClipboardHander

    private ProgressToastReporter? _progress;
    private readonly object _progressLocker = new();

    private readonly NotificationManager _notificationManager;
    private readonly ILogger _logger;
    private readonly ConfigManager _configManager;
    private readonly IClipboardFactory _clipboardFactory;
    private readonly IServiceProvider _serviceProvider;
    private ClipboardAssistConfig _clipboardAssistConfig;
    private IHttp Http => _serviceProvider.GetRequiredService<IHttp>();
    private IAppConfig AppConfig => _serviceProvider.GetRequiredService<IAppConfig>();
    private ITrayIcon TrayIcon => _serviceProvider.GetRequiredService<ITrayIcon>();

    public EasyCopyImageSerivce(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger>();
        _configManager = _serviceProvider.GetRequiredService<ConfigManager>();
        _clipboardFactory = _serviceProvider.GetRequiredService<IClipboardFactory>();
        _notificationManager = _serviceProvider.GetRequiredService<NotificationManager>();
        _clipboardAssistConfig = _configManager.GetConfig<ClipboardAssistConfig>(ConfigKey.ClipboardAssist) ?? new();
    }

    public override void Load()
    {
        _clipboardAssistConfig = _configManager.GetConfig<ClipboardAssistConfig>(ConfigKey.ClipboardAssist) ?? new();
        base.Load();
    }

    private async Task ProcessClipboard(ClipboardMetaInfomation metaInfo, bool useProxy, CancellationToken cancellationToken)
    {
        TrayIcon.SetStatusString(SERVICE_NAME, "Running.");
        var profile = _clipboardFactory.CreateProfile(metaInfo);
        if (profile.Type != ProfileType.Image || !NeedAdjust(metaInfo))
        {
            return;
        }

        if (DownloadWebImageEnabled && !string.IsNullOrEmpty(metaInfo.Html))
        {
            TrayIcon.SetStatusString(SERVICE_NAME, "Downloading web image.");
            const string Expression = @"<!--StartFragment--><img src=(?<qoute>[""'])(?<imgUrl>https?://.*?)\k<qoute>.*/><!--EndFragment-->";
            var match = Regex.Match(metaInfo.Html, Expression, RegexOptions.Compiled);    // 性能未测试，benchmark参考 https://www.bilibili.com/video/av441496306/?p=1&plat_id=313&t=15m53s
            if (match.Success) // 是从浏览器复制的图片
            {
                _logger.Write(LOG_TAG, "http image url: " + match.Groups["imgUrl"].Value);
                var localPath = await DownloadImage(match.Groups["imgUrl"].Value, useProxy, cancellationToken);
                if (!ImageHelper.FileIsImage(localPath))
                {
                    TrayIcon.SetStatusString(SERVICE_NAME, "Converting Complex image.");
                    localPath = await ImageHelper.CompatibilityCast(localPath, AppConfig.LocalTemplateFolder, cancellationToken);
                }
                profile = new ImageProfile(localPath, _serviceProvider);
            }
        }

        await AdjustClipboard(profile, cancellationToken);
        TrayIcon.SetStatusString(SERVICE_NAME, "Running.");
    }

    private static bool NeedAdjust(ClipboardMetaInfomation metaInfo)
    {
        if (metaInfo.Files?.Length > 1)
        {
            return false;
        }

        if ((metaInfo.Effects & DragDropEffects.Move) == DragDropEffects.Move)
        {
            return false;
        }
        return metaInfo.Files is null || metaInfo.Html is null || metaInfo.Image is null;
    }

    private static async Task AdjustClipboard(Profile profile, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                profile.SetLocalClipboard();
                break;
            }
            catch
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }

    private async Task<string> DownloadImage(string imageUrl, bool useProxy, CancellationToken cancellationToken)
    {
        var filename = Regex.Match(imageUrl, "[^/]+(?!.*/)");
        lock (_progressLocker)
        {
            _progress ??= new(filename.Value[..Math.Min(filename.Value.Length, 50)], "正在从网站下载原图", _notificationManager);
        }
        if (useProxy)
        {
            var fullPath = Path.Combine(AppConfig.LocalTemplateFolder, "proxy " + filename.Value);
            await Http.GetFile(imageUrl, fullPath, _progress, cancellationToken, true);
            return fullPath;
        }
        else
        {
            var fullPath = Path.Combine(AppConfig.LocalTemplateFolder, filename.Value);
            await Http.GetFile(imageUrl, fullPath, _progress, cancellationToken);
            return fullPath;
        }
    }
}