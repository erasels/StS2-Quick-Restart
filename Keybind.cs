using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace QuickRestart;

[HarmonyPatch(typeof(NGame), "_Input")]
public class Keybind
{
    private static ulong HOLD_DUR = 2000;

    private static ulong _pressStartTime;

    private static bool _isPressed;

    private static bool _triggered;

    private static void Postfix(InputEvent inputEvent)
    {
        // Skip logic if feedback screen is open
        if (inputEvent is InputEventKey)
        {
            NGame? instance = NGame.Instance;
            if (instance is { FeedbackScreen.Visible: true })
            {
                return;
            }
        }

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
            if (num >= HOLD_DUR)
            {
                _triggered = true;
                QuickRestart.RestartRoom();
            }
        }
    }
}