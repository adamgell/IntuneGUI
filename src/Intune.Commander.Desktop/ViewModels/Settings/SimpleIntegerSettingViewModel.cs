using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class SimpleIntegerSettingViewModel : SettingViewModelBase
{
    [ObservableProperty]
    private long? _value;

    public long? Minimum { get; init; }

    public long? Maximum { get; init; }

    partial void OnValueChanged(long? value) => IsModified = true;

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        return WrapInstance(new DeviceManagementConfigurationSimpleSettingInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            SimpleSettingValue = new DeviceManagementConfigurationIntegerSettingValue
            {
                Value = (int?)Value
            }
        });
    }
}
