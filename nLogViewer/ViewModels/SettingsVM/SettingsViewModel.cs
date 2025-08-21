using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.Model.AppSettings.AppConfig;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels.SettingsVM;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAppConfig _appConfig;
    
    public SettingsViewModel(IAppConfig appConfig)
    {
        _appConfig = appConfig;
        InitializeSettings();
        InitializeCommands();
    }

    #region Properties

    private bool _enableRowVirtualization;
    public bool EnableRowVirtualization
    {
        get => _enableRowVirtualization;
        set => Set(ref _enableRowVirtualization, value);
    }

    private bool _enablePanelVirtualization;
    public bool EnablePanelVirtualization
    {
        get => _enablePanelVirtualization;
        set => Set(ref _enablePanelVirtualization, value);
    }

    private bool _enableCanContentScroll;
    public bool EnableCanContentScroll
    {
        get => _enableCanContentScroll;
        set => Set(ref _enableCanContentScroll, value);
    }

    private string _virtualizationMode;
    public string VirtualizationMode
    {
        get => _virtualizationMode;
        set => Set(ref _virtualizationMode, value);
    }

    private int _maxItemsWithoutVirtualization;
    public int MaxItemsWithoutVirtualization
    {
        get => _maxItemsWithoutVirtualization;
        set => Set(ref _maxItemsWithoutVirtualization, value);
    }

    private bool _autoVirtualization;
    public bool AutoVirtualization
    {
        get => _autoVirtualization;
        set => Set(ref _autoVirtualization, value);
    }

    public ObservableCollection<string> VirtualizationModes { get; } = new()
    {
        "Recycling",
        "Standard"
    };

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }
    public ICommand ResetToDefaultCommand { get; private set; }

    #endregion

    #region Methods

    private void InitializeSettings()
    {
        var uiConfig = _appConfig.UIConfig;
        
        EnableRowVirtualization = uiConfig?.EnableRowVirtualization ?? true;
        EnablePanelVirtualization = uiConfig?.EnablePanelVirtualization ?? true;
        EnableCanContentScroll = uiConfig?.EnableCanContentScroll ?? true;
        VirtualizationMode = uiConfig?.VirtualizationMode ?? "Recycling";
        MaxItemsWithoutVirtualization = uiConfig?.MaxItemsWithoutVirtualization ?? 1000;
        AutoVirtualization = uiConfig?.AutoVirtualization ?? true;
    }

    private void InitializeCommands()
    {
        SaveCommand = new LambdaCommand(_ => SaveSettings(), _ => true);
        CancelCommand = new LambdaCommand(_ => CloseDialog(false), _ => true);
        ResetToDefaultCommand = new LambdaCommand(_ => ResetToDefaults(), _ => true);
    }

    private void SaveSettings()
    {
        if (_appConfig.UIConfig == null)
            return;

        _appConfig.UIConfig.EnableRowVirtualization = EnableRowVirtualization;
        _appConfig.UIConfig.EnablePanelVirtualization = EnablePanelVirtualization;
        _appConfig.UIConfig.EnableCanContentScroll = EnableCanContentScroll;
        _appConfig.UIConfig.VirtualizationMode = VirtualizationMode;
        _appConfig.UIConfig.MaxItemsWithoutVirtualization = MaxItemsWithoutVirtualization;
        _appConfig.UIConfig.AutoVirtualization = AutoVirtualization;

        CloseDialog(true);
    }

    private void ResetToDefaults()
    {
        EnableRowVirtualization = true;
        EnablePanelVirtualization = true;
        EnableCanContentScroll = true;
        VirtualizationMode = "Recycling";
        MaxItemsWithoutVirtualization = 1000;
        AutoVirtualization = true;
    }

    private void CloseDialog(bool result)
    {
        DialogResult = result;
        OnCloseRequested();
    }

    public bool? DialogResult { get; private set; }
    
    public event System.Action CloseRequested;
    
    private void OnCloseRequested()
    {
        CloseRequested?.Invoke();
    }

    #endregion
}