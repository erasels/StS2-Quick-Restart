using BaseLib.Config;

namespace QuickRestart.QuickRestartCode;

public class Config: SimpleModConfig
{
    [ConfigSlider(500, 3000, 100, Format = "{0}ms")]
    public static int HoldDur { get; set; } = 2000;

    public static bool ShowIndicator { get; set; } = true;
}
