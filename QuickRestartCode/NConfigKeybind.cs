using System.Reflection;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;

namespace QuickRestart.QuickRestartCode;

/// <summary>
/// A config option control for rebinding a key, by clicking the button and then pressing the key.
/// Behaves like the other BaseLib option controls.
/// </summary>
public partial class NConfigKeybind : NConfigButton
{
    private ModConfig? _config;
    private PropertyInfo? _property;
    private MegaLabel? _keyLabel;
    private bool _listening;

    public void Initialize(ModConfig config, PropertyInfo property)
    {
        _config = config;
        _property = property;

        base.Initialize(string.Empty, OnClicked);

        _keyLabel = GetNodeOrNull<MegaLabel>("Label");
        UpdateLabel();

        config.OnConfigReloaded += UpdateLabel;
    }

    private void OnClicked()
    {
        _listening = !_listening;
        UpdateLabel();
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (!_listening)
        {
            return;
        }

        if (inputEvent is not InputEventKey key || !key.Pressed || key.IsEcho())
        {
            return;
        }

        GetViewport()?.SetInputAsHandled();
        _listening = false;

        if (key.Keycode == Key.Escape)
        {
            UpdateLabel();
            return;
        }

        _property?.SetValue(null, key.Keycode);
        _config?.Changed();
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (_keyLabel == null || _property == null || _config == null)
        {
            return;
        }

        if (_listening)
        {
            var locKey = _config.ModPrefix + "RESTART_KEY.listening";
            _keyLabel.Text = LocString.GetIfExists("settings_ui", locKey)?.GetFormattedText() ?? "Press a key...";
            return;
        }

        var current = (Key)(_property.GetValue(null) ?? Key.None);
        _keyLabel.Text = current == Key.None ? "-" : current.ToString();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_config != null)
        {
            _config.OnConfigReloaded -= UpdateLabel;
        }
    }
}