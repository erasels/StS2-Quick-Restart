using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;

namespace QuickRestart;
    
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

[HarmonyPatch]
public class QuickRestart
{
    private static String RestartButtonName = "QuickRestartButton";
    private static NPauseMenuButton _restartButton;
    
    [HarmonyPatch(typeof(NPauseMenu), "_Ready")]
    public class PauseMenuButtonPatch
    {
        [HarmonyPostfix]
        static void InitRestartButton(NPauseMenu __instance, Godot.Control ____buttonContainer, NPauseMenuButton ____settingsButton, NPauseMenuButton ____giveUpButton)
        {
            if (_restartButton == null || !GodotObject.IsInstanceValid(_restartButton))
            {
                CreateRestartButton(____buttonContainer, ____settingsButton, ____giveUpButton);
            }
            else
            {
                MainFile.Logger.Debug("Reusing previously created button in pause menu");
            }
        }
        
        private static void OnPressed()
        {
            RestartRoom();
        }

        private static void CreateRestartButton(Control btnContainer, NPauseMenuButton settingsBtn, NPauseMenuButton giveUpBtn)
        {
            MainFile.Logger.Debug("Creating a new save button");
            try
            {
                _restartButton = (NPauseMenuButton)settingsBtn.Duplicate();
                _restartButton.Name = RestartButtonName;

                // Duplicate the shader material on ButtonImage so hover isn't shared
                var image = _restartButton.GetNode<TextureRect>("ButtonImage");
                image.Material = (ShaderMaterial)image.Material.Duplicate();
                
                // Update the internal _hsv reference to point to the new material
                _restartButton._hsv = (ShaderMaterial)image.Material;
                
                // Add button above the give up button
                btnContainer.AddChild(_restartButton);
                var giveupIndex = giveUpBtn.GetIndex();
                btnContainer.MoveChild(_restartButton, giveupIndex);

                LocString loc = new LocString("gameplay_ui", "QR_RESTART_BUTTON");
                _restartButton.GetNode<MegaLabel>("Label").SetTextAutoSize(loc.GetFormattedText());
                _restartButton.Enable();
                _restartButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnPressed()));
            }
            catch (Exception e)
            {
                MainFile.Logger.Error($"Ran into error during restart button creation: \n{e.Message}\n{e.StackTrace}");
            }
        }
    }

    [HarmonyPatch(typeof(NPauseMenu), "Initialize")]
    public class OnPauseMenuOpen
    {
        [HarmonyPostfix]
        public static void Postfix(NPauseMenu __instance)
        {
            if (_restartButton != null && GodotObject.IsInstanceValid(_restartButton))
            {
                if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer ||
                    __instance._runState.IsGameOver)
                {
                    _restartButton.Disable();
                }
                else
                {
                    _restartButton.Enable();
                }
            }
        }
    }

    /// <summary>
    /// Restarts the current room in single player. Restarting works by mimicking exiting the run and then loading it.
    /// </summary>
    public static void RestartRoom()
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Singleplayer)
        {
            MainFile.Logger.Error("Not in singleplayer!!! How did this get called");
            return;
        }

        if (!SaveManager.Instance.HasRunSave)
        {
            MainFile.Logger.Error("We don't have a run save, aborting");
            return;
        }

        // Cleaning up the current room
        RunManager.Instance.ActionQueueSet.Reset();
        NRunMusicController.Instance.StopMusic();
        RunManager.Instance.CleanUp();

        MainFile.Logger.Info("Cleaned up, starting load now");

        // Loads run data
        ReadSaveResult<SerializableRun> runSave = SaveManager.Instance.LoadRunSave();
        SerializableRun serializableRun = runSave.SaveData;
        RunState runState = RunState.FromSerializable(serializableRun);

        MainFile.Logger.Info("Managed to load run data");

        // Make use of run data to reload current run
        RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
        MainFile.Logger.Info($"Continuing run with character: {serializableRun.Players[0].CharacterId}");
        SfxCmd.Play(runState.Players[0].Character.CharacterTransitionSfx);
        NGame.Instance.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
        TaskHelper.RunSafely(NGame.Instance.LoadRun(runState, serializableRun.PreFinishedRoom));
    }
}