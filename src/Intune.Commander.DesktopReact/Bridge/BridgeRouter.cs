using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Intune.Commander.DesktopReact.Services;
using Microsoft.Web.WebView2.Core;

namespace Intune.Commander.DesktopReact.Bridge;

public class BridgeRouter : IBridgeService
{
    private CoreWebView2? _webView;
    private readonly ProfileBridgeService _profileBridge;
    private readonly AuthBridgeService _authBridge;
    private readonly NavigationBridgeService _navBridge;
    private readonly ShellStateBridgeService _shellBridge;
    private readonly SettingsCatalogBridgeService _settingsCatalogBridge;
    private readonly SearchBridgeService _searchBridge;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BridgeRouter(
        ProfileBridgeService profileBridge,
        AuthBridgeService authBridge,
        NavigationBridgeService navBridge,
        ShellStateBridgeService shellBridge,
        SettingsCatalogBridgeService settingsCatalogBridge,
        SearchBridgeService searchBridge)
    {
        _profileBridge = profileBridge;
        _authBridge = authBridge;
        _navBridge = navBridge;
        _shellBridge = shellBridge;
        _settingsCatalogBridge = settingsCatalogBridge;
        _searchBridge = searchBridge;
    }

    public void Initialize(CoreWebView2 webView)
    {
        _webView = webView;
        _webView.WebMessageReceived += OnWebMessageReceived;

        // Give bridge services access to push events
        _profileBridge.SetBridge(this);
        _authBridge.SetBridge(this);
        _shellBridge.SetBridge(this);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        BridgeCommand? command;
        try
        {
            var json = e.WebMessageAsJson;
            command = JsonSerializer.Deserialize<BridgeCommand>(json, JsonOptions);
        }
        catch
        {
            return; // Malformed message — ignore
        }

        if (command is null || command.Protocol != "ic/1")
            return;

        try
        {
            var result = command.Command switch
            {
                "profiles.load" => await _profileBridge.LoadAsync(),
                "profiles.save" => await _profileBridge.SaveAsync(command.Payload),
                "profiles.delete" => await _profileBridge.DeleteAsync(command.Payload),
                "profiles.import" => await _profileBridge.ImportAsync(),
                "auth.connect" => await _authBridge.ConnectAsync(command.Payload),
                "auth.disconnect" => await _authBridge.DisconnectAsync(),
                "nav.getCategories" => _navBridge.GetCategories(),
                "shell.getState" => _shellBridge.GetState(),
                "settingsCatalog.list" => await _settingsCatalogBridge.ListAsync(),
                "settingsCatalog.getDetail" => await _settingsCatalogBridge.GetDetailAsync(command.Payload),
                "search.query" => await _searchBridge.SearchAsync(command.Payload),
                _ => throw new NotSupportedException($"Unknown command: {command.Command}")
            };

            await SendResponseAsync(command.Id, true, result);
        }
        catch (Exception ex)
        {
            await SendResponseAsync(command.Id, false, null, ex.Message);
        }
    }

    public Task SendEventAsync(string eventName, object payload)
    {
        if (_webView is null) return Task.CompletedTask;

        var evt = BridgeEvent.Create(eventName, payload);
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        // Must dispatch to UI thread for WebView2
        Application.Current.Dispatcher.Invoke(() => _webView.PostWebMessageAsJson(json));
        return Task.CompletedTask;
    }

    public Task SendResponseAsync(string id, bool success, object? payload, string? error = null)
    {
        if (_webView is null) return Task.CompletedTask;

        var response = success
            ? BridgeResponse.Ok(id, payload)
            : BridgeResponse.Fail(id, error ?? "Unknown error");

        var json = JsonSerializer.Serialize(response, JsonOptions);

        Application.Current.Dispatcher.Invoke(() => _webView.PostWebMessageAsJson(json));
        return Task.CompletedTask;
    }
}
