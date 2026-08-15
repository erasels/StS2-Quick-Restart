using System.Reflection;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;

namespace QuickRestart.QuickRestartCode;

public class Config: SimpleModConfig
{
    [ConfigSlider(500, 3000, 100, Format = "{0}ms")]
    public static int HoldDur { get; set; } = 2000;

    public static bool ShowIndicator { get; set; } = true;

    // Bound via the custom keybind row built in SetupConfigUI. Marked hidden so the auto-generator
    // doesn't render it as a dropdown of every Godot.Key lol
    [ConfigHideInUI]
    public static Key RestartKey { get; set; } = Key.R;

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);

        var property = GetType().GetProperty(nameof(RestartKey))!;

        optionContainer.AddChild(CreateDividerControl());

        var keybind = new NConfigKeybind();
        keybind.Initialize(this, property);

        var label = CreateRawLabelControl(GetLabelText(nameof(RestartKey)), 28);
        var row = new NConfigOptionRow(ModPrefix, nameof(RestartKey), label, keybind);
        row.UniqueNameInOwner = true;
        optionContainer.AddChild(row);
        row.Owner = optionContainer;

        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }
}