using Godot;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace QuickRestart.QuickRestartCode;

/// <summary>
/// A radial progress circle that follows the mouse cursor while the reload key is held.
/// Uses Godot's <see cref="TextureProgressBar"/> with radial fill.
/// </summary>
public partial class HoldProgressIndicator : TextureProgressBar
{
    private const int TextureSize = 48;
    private const float OuterRadius = 22f;
    private const float InnerRadius = 15f;

    private const float CursorOffsetX = 20f;
    private const float CursorOffsetY = 20f;

    // Indicator fades in this long to make it less jarring
    private const double FadeInDuration = 0.2;

    private static readonly Color ProgressColor = new(1f, 0.85f, 0.35f);
    private static readonly Color UnderColor = new(1f, 1f, 1f, 0.5f);

    public static HoldProgressIndicator? Instance { get; internal set; }

    private double _fadeTimer;

    public static void EnsureCreated()
    {
        if (Instance != null && IsInstanceValid(Instance))
        {
            return;
        }

        try
        {
            var game = NGame.Instance;
            if (game == null || !IsInstanceValid(game))
            {
                return;
            }

            var indicator = new HoldProgressIndicator
            {
                Name = "QuickRestartHoldIndicator"
            };
            game.AddChildSafely(indicator);
            Instance = indicator;
            MainFile.Logger.Info("Created hold-to-restart progress indicator");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Failed to create hold progress indicator:\n{e.Message}\n{e.StackTrace}");
        }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        NinePatchStretch = false;
        FillMode = (int)FillModeEnum.Clockwise;
        RadialInitialAngle = -90f;
        RadialFillDegrees = 360f;
        MinValue = 0;
        Step = 0;
        MaxValue = Config.HoldDur;

        TextureUnder = MakeRing(UnderColor);
        TextureProgress = MakeRing(ProgressColor);

        CustomMinimumSize = new Vector2(TextureSize, TextureSize);
        Size = new Vector2(TextureSize, TextureSize);
        ZIndex = 1000;
        Visible = false;
        Value = 0;

        // Start fully transparent, alpha is controlled by the fade-in during _Process
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public override void _Process(double delta)
    {
        if (!Restarter.CanRestart())
        {
            HideAndReset();
            return;
        }

        if (Keybind.IsHolding && !Keybind.Triggered)
        {
            int hold = Config.HoldDur;
            MaxValue = hold;

            ulong elapsed = Time.GetTicksMsec() - Keybind.PressStartTime;
            ulong holdU = (ulong)hold;

            if (Config.ShowIndicator)
            {
                if (!Visible)
                {
                    Visible = true;
                }

                // Fade in over the first FadeInDuration seconds
                _fadeTimer = Math.Min(_fadeTimer + delta, FadeInDuration);
                float alpha = (float)(_fadeTimer / FadeInDuration);
                Modulate = new Color(1f, 1f, 1f, alpha);

                GlobalPosition = GetViewport().GetMousePosition() + new Vector2(CursorOffsetX, CursorOffsetY);
            }

            Value = Mathf.Min((float)elapsed, (float)hold);

            if (elapsed >= holdU)
            {
                Keybind.Triggered = true;
                HideAndReset();
                TaskHelper.RunSafely(Restarter.RestartRoomAsync());
            }
        }
        else
        {
            HideAndReset();
        }
    }

    private void HideAndReset()
    {
        Value = 0;
        _fadeTimer = 0.0;
        Modulate = new Color(1f, 1f, 1f, 0f);
        if (Visible)
        {
            Visible = false;
        }
    }

    /// <summary>
    /// Builds a hollow ring, used as the radial fill texture.
    /// </summary>
    private static ImageTexture MakeRing(Color color)
    {
        var image = Image.CreateEmpty(TextureSize, TextureSize, false, Image.Format.Rgba8);
        float center = TextureSize / 2f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float outerEdge = Mathf.Clamp(OuterRadius + 1f - dist, 0f, 1f);
                float innerEdge = Mathf.Clamp(dist - (InnerRadius - 1f), 0f, 1f);
                float alpha = Mathf.Min(outerEdge, innerEdge);

                if (alpha <= 0f)
                {
                    continue;
                }

                image.SetPixel(x, y, new Color(color.R, color.G, color.B, alpha * color.A));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}