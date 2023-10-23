﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyncClipboard.Core.Commons;
using SyncClipboard.Core.Interfaces;
using SyncClipboard.Core.Models.UserConfigs;

namespace SyncClipboard.Core.ViewModels;

public partial class SyncSettingViewModel : ObservableObject
{
    #region server
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNormalClientEnable))]
    private bool serverEnable;
    partial void OnServerEnableChanged(bool value) => ServerConfig = ServerConfig with { SwitchOn = value };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNormalClientEnable))]
    private bool clientMixedMode;
    partial void OnClientMixedModeChanged(bool value) => ServerConfig = ServerConfig with { ClientMixedMode = value };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServerConfigDescription))]
    private ServerConfig serverConfig;
    partial void OnServerConfigChanged(ServerConfig value)
    {
        _configManager.SetConfig(ConfigKey.Server, value);
    }

    #endregion

    #region client
    [ObservableProperty]
    private bool syncEnable;
    partial void OnSyncEnableChanged(bool value) => ClientConfig = ClientConfig with { SyncSwitchOn = value };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserManulServer))]
    private bool useLocalServer;
    partial void OnUseLocalServerChanged(bool value) => ClientConfig = ClientConfig with { UseLocalServer = value };

    [ObservableProperty]
    private uint intervalTime;
    partial void OnIntervalTimeChanged(uint value) => ClientConfig = ClientConfig with { IntervalTime = value };

    [ObservableProperty]
    private uint timeOut;
    partial void OnTimeOutChanged(uint value) => ClientConfig = ClientConfig with { TimeOut = value };

    [ObservableProperty]
    private uint maxFileSize;
    partial void OnMaxFileSizeChanged(uint value) => ClientConfig = ClientConfig with { MaxFileByte = value * 1024 * 1024 };

    [ObservableProperty]
    private bool autoDeleleServerFile;
    partial void OnAutoDeleleServerFileChanged(bool value) => ClientConfig = ClientConfig with { DeletePreviousFilesOnPush = value };

    [ObservableProperty]
    private bool notifyOnDownloaded;
    partial void OnNotifyOnDownloadedChanged(bool value) => ClientConfig = ClientConfig with { NotifyOnDownloaded = value };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntervalTime))]
    [NotifyPropertyChangedFor(nameof(SyncEnable))]
    [NotifyPropertyChangedFor(nameof(UseLocalServer))]
    [NotifyPropertyChangedFor(nameof(TimeOut))]
    [NotifyPropertyChangedFor(nameof(ClientConfigDescription))]
    private SyncConfig clientConfig;
    partial void OnClientConfigChanged(SyncConfig value)
    {
        IntervalTime = value.IntervalTime;
        SyncEnable = value.SyncSwitchOn;
        UseLocalServer = value.UseLocalServer;
        TimeOut = value.TimeOut;
        MaxFileSize = value.MaxFileByte / 1024 / 1024;
        AutoDeleleServerFile = value.DeletePreviousFilesOnPush;
        NotifyOnDownloaded = value.NotifyOnDownloaded;
        _configManager.SetConfig(ConfigKey.Sync, value);
    }

    [RelayCommand]
    private void LoginWithNextcloud()
    {
        _mainVM.BreadcrumbList.Add(PageDefinition.NextCloudLogIn);
        _mainVM.NavigateTo(PageDefinition.NextCloudLogIn, NavigationTransitionEffect.FromRight);
    }

    #endregion

    #region for view only

    public bool IsNormalClientEnable => !ServerEnable || !ClientMixedMode;

    public bool UserManulServer => !UseLocalServer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServerConfigDescription))]
    public bool showServerPassword = false;

    public string ServerConfigDescription =>
@$"{I18n.Strings.Port}{new string('\t', int.Parse(I18n.Strings.PortTabRepeat))}: {ServerConfig.Port}
{I18n.Strings.UserName}{new string('\t', int.Parse(I18n.Strings.UserNameTabRepeat))}: {ServerConfig.UserName}
{I18n.Strings.Password}{new string('\t', int.Parse(I18n.Strings.PasswordTabRepeat))}: {GetPasswordString(ServerConfig.Password, ShowServerPassword)}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientConfigDescription))]
    public bool showClientPassword = false;

    public string ClientConfigDescription =>
@$"{I18n.Strings.Address}{new string('\t', int.Parse(I18n.Strings.PortTabRepeat))}: {ClientConfig.RemoteURL}
{I18n.Strings.UserName}{new string('\t', int.Parse(I18n.Strings.UserNameTabRepeat))}: {ClientConfig.UserName}
{I18n.Strings.Password}{new string('\t', int.Parse(I18n.Strings.PasswordTabRepeat))}: {GetPasswordString(ClientConfig.Password, ShowClientPassword)}";

    private static string GetPasswordString(string origin, bool? show)
    {
        return show ?? false ? origin : "*********";
    }

    #endregion

    private readonly ConfigManager _configManager;
    private readonly MainViewModel _mainVM;

    public SyncSettingViewModel(ConfigManager configManager, MainViewModel mainViewModel)
    {
        _configManager = configManager;
        _mainVM = mainViewModel;
        _configManager.ListenConfig<ServerConfig>(ConfigKey.Server, LoadSeverConfig);
        serverConfig = _configManager.GetConfig<ServerConfig>(ConfigKey.Server) ?? new();
        serverEnable = serverConfig.SwitchOn;
        clientMixedMode = serverConfig.ClientMixedMode;

        _configManager.ListenConfig<SyncConfig>(ConfigKey.Sync, LoadClientConfig);
        ClientConfig = _configManager.GetConfig<SyncConfig>(ConfigKey.Sync) ?? new();
    }

    private void LoadSeverConfig(object? config)
    {
        ServerConfig = config as ServerConfig ?? new();
        ServerEnable = ServerConfig.SwitchOn;
        ClientMixedMode = ServerConfig.ClientMixedMode;
    }

    private void LoadClientConfig(object? config)
    {
        ClientConfig = config as SyncConfig ?? new();
    }

    public string? SetServerConfig(string portString, string username, string password)
    {
        if (!ushort.TryParse(portString, out var port))
        {
            return I18n.Strings.PortRangeIs;
        }
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return I18n.Strings.UsernameOrPasswordBlank;
        }

        ServerConfig = ServerConfig with { Password = password, Port = port, UserName = username };

        return null;
    }

    public string? SetClientConfig(string url, string username, string password)
    {
        ClientConfig = ClientConfig with { RemoteURL = url, UserName = username, Password = password };
        return null;
    }
}
