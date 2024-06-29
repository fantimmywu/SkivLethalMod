using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using SkivMultimod.Patches;

namespace SkivMod
{
    public class PLUGIN_INFO
    {
        public const string PLUGIN_GUID = "Skiv.Multimod";
        public const string PLUGIN_NAME = "Multimods";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [BepInPlugin(PLUGIN_INFO.PLUGIN_GUID, PLUGIN_INFO.PLUGIN_NAME, PLUGIN_INFO.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PLUGIN_INFO.PLUGIN_GUID);
        internal static ManualLogSource mls;

        // Configs
        private InputAction insertKeyAction;
        private static ConfigEntry<string> speedKeyBind;

        private void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_INFO.PLUGIN_GUID);
            mls.LogInfo($"{PLUGIN_INFO.PLUGIN_NAME} now loaded");

            setBinding();

            // Enable keybindings
            insertKeyAction = new InputAction(binding: $"<Keyboard>/{speedKeyBind.Value}");
            insertKeyAction.performed += OnInsertKeyPressed;
            insertKeyAction.Enable();

            // Patches
            System.Type[] patches = new System.Type[] {
                typeof(Plugin), typeof(SpeedPatch), typeof(JumpPatch)
            };
            foreach (System.Type patch in patches) harmony.PatchAll(patch);

        }

        private void setBinding()
        {
            speedKeyBind = Config.Bind("Hotkey", "Toggle Key", "N", "Hotkey to toggle speed/jumps");
        }

        private void OnInsertKeyPressed(InputAction.CallbackContext obj)
        {
            PlayerControllerB player = StartOfRound.Instance?.localPlayerController;
            if (player == null || player.inTerminalMenu || player.isTypingChat) return;

            SpeedPatch.toggled = !SpeedPatch.toggled;
            JumpPatch.toggled = !JumpPatch.toggled;
            mls.LogInfo($"Speed Patch {(SpeedPatch.toggled ? "enabled" : "disabled")}");
            mls.LogInfo($"Jump Patch {(JumpPatch.toggled ? "enabled" : "disabled")}");
        }
    }
}

namespace SkivMultimod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class SpeedPatch
    {
        internal static bool toggled;
        [HarmonyPostfix]
        private static void speedPatch(PlayerControllerB __instance)
        {
            if (toggled)
            {
                __instance.movementSpeed = 10f;
            }
            else
            {
                __instance.movementSpeed = 5f;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class JumpPatch
    {
        // Based on https://thunderstore.io/c/lethal-company/p/PugRoll/TripleJump/
        // Modified for infinite jumps
        internal static bool toggled;
        [HarmonyPostfix]
        public static void infiniteJump(ref bool ___isJumping, ref bool ___isFallingFromJump, ref bool ___isExhausted, PlayerControllerB __instance, ref float ___sprintMeter, ref bool ___isFallingNoJump)
        {
            if (toggled && !___isExhausted && (___isFallingFromJump | ___isFallingNoJump) && ((ButtonControl)Keyboard.current.spaceKey).wasPressedThisFrame)
            {
                __instance.StartCoroutine("PlayerJump");
                //___sprintMeter = Mathf.Clamp(___sprintMeter - 0.08f, 0f, 1f);
                ___isJumping = true;
            }

            // Saved for future use when implementing UI, customize # of jumps maximum
            //public static int limitJumps = 2; declare before function
            //if (__instance.thisController.isGrounded)
            //{
            //    limitJumps = 0;
            //}
        }

    }
}