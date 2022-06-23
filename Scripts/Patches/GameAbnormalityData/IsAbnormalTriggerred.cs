using ABN;
using HarmonyLib;

namespace GalacticScale
{
    public static partial class PatchOnGameAbnormalityData
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameAbnormalityData_0925), "IsAbnormalTriggerred")]
        public static bool IsAbnormalityTriggered(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}