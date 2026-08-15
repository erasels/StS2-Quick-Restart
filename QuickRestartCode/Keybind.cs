using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace QuickRestart.QuickRestartCode;

[HarmonyPatch(typeof(NGame), "_Input")]
public class Keybind
{
    private static ulong _pressStartTime;

    private static bool _isPressed;

    private static bool _triggered;

    private static void Postfix(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Keycode: Key.R } inputKey)
        {
            switch (inputKey.Pressed)
            {
                case true when !inputKey.IsEcho():
                    _pressStartTime = Time.GetTicksMsec();
                    _isPressed = true;
                    _triggered = false;
                    break;
                case false:
                    _isPressed = false;
                    _triggered = false;
                    break;
            }
        }

        if (_isPressed && !_triggered)
        {
            var num = Time.GetTicksMsec() - _pressStartTime;
            if (Convert.ToInt32(num.ToString()) >= Config.HoldDur)
            {
                _triggered = true;
                TaskHelper.RunSafely(Restarter.RestartRoomAsync());
            }
        }
    }
}