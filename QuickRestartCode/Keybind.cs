using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace QuickRestart.QuickRestartCode;

[HarmonyPatch(typeof(NGame), "_Input")]
public class Keybind
{
    public static ulong PressStartTime;
    public static bool IsHolding;
    public static bool Triggered;

    private static void Postfix(InputEvent inputEvent)
    {
        HoldProgressIndicator.EnsureCreated();

        if (inputEvent is not InputEventKey { Keycode: Key.R } inputKey)
        {
            return;
        }

        switch (inputKey.Pressed)
        {
            case true when !inputKey.IsEcho():
                PressStartTime = Time.GetTicksMsec();
                IsHolding = true;
                Triggered = false;
                break;
            case false:
                IsHolding = false;
                Triggered = false;
                break;
        }
    }
}
