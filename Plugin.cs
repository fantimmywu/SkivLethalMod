using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using SkivMultimod.Patches;
using SkivMod;

namespace SkivMod
{
    public class PLUGIN_INFO
    {
        public const string PLUGIN_GUID = "Skiv.Multimod";
        public const string PLUGIN_NAME = "Multimods";
        public const string PLUGIN_VERSION = "1.2.0";
    }

    [BepInPlugin(PLUGIN_INFO.PLUGIN_GUID, PLUGIN_INFO.PLUGIN_NAME, PLUGIN_INFO.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Harmony harmony = new Harmony(PLUGIN_INFO.PLUGIN_GUID);
        internal static ManualLogSource mls;

        // Configs
        private InputAction insertKeyAction;
        private static ConfigEntry<string> modKeyBind;

        // Speed
        public static ConfigEntry<bool> enableSpeedMod;
        public static ConfigEntry<bool> enableStaminaMod;
        public static ConfigEntry<float> moveSpeedValue;
        // Jump
        public static ConfigEntry<bool> enableJumpMod;
        public static ConfigEntry<int> maxJumpLimit;
        public static ConfigEntry<bool> noFallToggle;
        // Reach
        public static ConfigEntry<bool> enableReachMod;
        // God
        public static ConfigEntry<bool> enableGodMod;

        private void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_INFO.PLUGIN_GUID);
            mls.LogInfo($"{PLUGIN_INFO.PLUGIN_NAME} now loaded");

            setBinding();

            // Enable keybindings
            insertKeyAction = new InputAction(binding: $"<Keyboard>/{modKeyBind.Value}");
            insertKeyAction.performed += OnInsertKeyPressed;
            insertKeyAction.Enable();

            // Patches
            System.Type[] patches = new System.Type[] {
                typeof(Plugin), typeof(SpeedPatch), typeof(JumpPatch), typeof(ReachPatch), typeof(GodPatch)
            };
            foreach (System.Type patch in patches) harmony.PatchAll(patch);

        }

        private void setBinding()
        {
            modKeyBind = Config.Bind("Hotkey", "Toggle Mod", "N", "Hotkey to toggle mods");
            // Speed
            enableSpeedMod = Config.Bind("Speed", "Enable speed mod", true, "Enable speed mod");
            enableStaminaMod = Config.Bind("Speed", "Disable stamina consumption", true, "Disables consuming stamina");
            moveSpeedValue = Config.Bind("Speed", "Move Speed", 9.2f, "Change movement speed");
            // Jump
            enableJumpMod = Config.Bind("Jump", "Enable jump mod", true, "Enable custom jumps");
            maxJumpLimit = Config.Bind("Jump", "Max jumps", -1, "Max multi-jumps (-1 for no limit)");
            noFallToggle = Config.Bind("Jump", "No fall", true, "Enable no fall");
            // Reach
            enableReachMod = Config.Bind("Reach", "Enable reach", false, "Enable long reach");
            // God
            enableGodMod = Config.Bind("Godmode", "Enable god mode", false, "Enable god mode");
        }

        private void OnInsertKeyPressed(InputAction.CallbackContext obj)
        {
            PlayerControllerB player = StartOfRound.Instance?.localPlayerController;
            if (player == null || player.inTerminalMenu || player.isTypingChat) return;

            SpeedPatch.toggled = !SpeedPatch.toggled;
            JumpPatch.toggled = !JumpPatch.toggled;
            ReachPatch.toggled = !ReachPatch.toggled;
            GodPatch.toggled = !GodPatch.toggled;
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
            // Speed
            if (toggled && Plugin.enableSpeedMod.Value)
            {
                __instance.movementSpeed = Plugin.moveSpeedValue.Value;
            }
            else
            {
                __instance.movementSpeed = 4.6f; // Default speed is 4.6f
            }
            // Stamina consumption
            if (toggled && Plugin.enableStaminaMod.Value)
            {
                __instance.sprintMeter = 1f;
            }
            
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class JumpPatch
    {
        // Initially based on https://thunderstore.io/c/lethal-company/p/PugRoll/TripleJump/
        // Modified for custom jumps and no fall
        internal static bool toggled;
        internal static int maxJumps = Plugin.maxJumpLimit.Value;
        [HarmonyPostfix]
        public static void infiniteJump(ref bool ___isJumping, ref bool ___isFallingFromJump, ref bool ___isExhausted, PlayerControllerB __instance, ref float ___sprintMeter, ref bool ___isFallingNoJump)
        {

            // No fall damage
            __instance.takingFallDamage = !(toggled && Plugin.noFallToggle.Value);


            // Jump mod
            if (!Plugin.enableJumpMod.Value) return;
            if (toggled && (maxJumps > 0 || maxJumps < 0) && (___isFallingFromJump | ___isFallingNoJump) && ((ButtonControl)Keyboard.current.spaceKey).wasPressedThisFrame)
            {
                maxJumps--;
                __instance.StartCoroutine("PlayerJump");
                ___isJumping = true;
            }
            if (__instance.thisController.isGrounded)
            {
                maxJumps = Plugin.maxJumpLimit.Value;
            }

        }

    }

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    internal class ReachPatch
    {
        internal static bool toggled;
        [HarmonyPostfix]
        private static void reachPatch(PlayerControllerB __instance)
        {
            if (toggled && Plugin.enableReachMod.Value)
            {
                __instance.grabDistance = float.MaxValue;
            }
            else
            {
                __instance.grabDistance = 3f;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    [HarmonyPatch("AllowPlayerDeath")]
    internal class GodPatch
    {
        internal static bool toggled;
        [HarmonyPrefix]
        static bool OverrideDeath()
        {
            return !(toggled && Plugin.enableGodMod.Value);
        }
    }
    


}