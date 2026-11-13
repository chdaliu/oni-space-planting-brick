using HarmonyLib;
using UnityEngine;

namespace SpacePlantingBrick
{
    /// <summary>
    /// The brick maintains its own internal growing environment in every
    /// environment mode, so a crop inside must not wilt because of the
    /// external gas pressure or gas type. PressureVulnerable still runs
    /// normally for plants outside the brick.
    ///
    /// The postfix runs after PressureVulnerable has read the real cell data,
    /// so the state machine sees the brick-provided pressure instead.
    /// </summary>
    [HarmonyPatch(typeof(PressureVulnerable), nameof(PressureVulnerable.SlicedSim1000ms))]
    public static class PressureVulnerablePatch
    {
        private const float PreferredInternalPressure = 2f;

        public static void Postfix(PressureVulnerable __instance)
        {
            SpacePlantingBrick brick = SpacePlantingBrick.FindForPlant(__instance.gameObject);
            if (brick == null || __instance.smi == null)
            {
                return;
            }

            float pressure = PreferredInternalPressure;
            float low = __instance.pressureWarning_Low;
            float high = __instance.pressureWarning_High;
            if (high > low)
            {
                pressure = Mathf.Clamp(pressure, low + 0.01f, high - 0.01f);
            }

            __instance.smi.sm.pressure.Set(pressure, __instance.smi);
            __instance.smi.sm.safe_element.Set(true, __instance.smi);
            __instance.testAreaElementSafe = true;
        }
    }

    /// <summary>
    /// The brick provides its own light/darkness conditions. External light
    /// (for example a lamp next to a darkness-loving crop) must not push the
    /// plant into a too-bright state, so crops in the brick always count as
    /// having safe illumination.
    /// </summary>
    [HarmonyPatch(typeof(IlluminationVulnerable), nameof(IlluminationVulnerable.IsCellSafe))]
    public static class IlluminationVulnerablePatch
    {
        public static void Postfix(IlluminationVulnerable __instance, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            if (SpacePlantingBrick.FindForPlant(__instance.gameObject) != null)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Liquid may enter the chamber in Gas + Liquid Permeable mode, but the
    /// brick still keeps the crop growing normally. Crops in the brick are
    /// therefore never treated as drowning.
    /// </summary>
    [HarmonyPatch(typeof(DrowningMonitor), nameof(DrowningMonitor.IsCellSafe))]
    public static class DrowningMonitorPatch
    {
        public static void Postfix(DrowningMonitor __instance, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            if (SpacePlantingBrick.FindForPlant(__instance.gameObject) != null)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// The brick actively holds the crop at the midpoint of its comfortable
    /// temperature range. Clamp the state machine's reading as well so a
    /// hot/cold cell cannot push the crop into a warning state between
    /// maintenance ticks.
    /// </summary>
    [HarmonyPatch(typeof(TemperatureVulnerable),
        nameof(TemperatureVulnerable.SlicedSim1000ms))]
    public static class TemperatureVulnerablePatch
    {
        public static void Postfix(TemperatureVulnerable __instance)
        {
            if (SpacePlantingBrick.FindForPlant(__instance.gameObject) == null
                || __instance.smi == null || __instance.smi.sm == null)
            {
                return;
            }

            float low = __instance.TemperatureWarningLow;
            float high = __instance.TemperatureWarningHigh;
            if (high > low)
            {
                __instance.smi.sm.internalTemp.Set(
                    (low + high) * 0.5f, __instance.smi);
            }
        }
    }

    /// <summary>
    /// In radiation-enabled saves the brick supplies a suitable internal
    /// environment, so external radiation must not wilt the crop.
    /// </summary>
    [HarmonyPatch(typeof(RadiationVulnerable.StatesInstance),
        nameof(RadiationVulnerable.StatesInstance.GetRadiationThresholdCrossed))]
    public static class RadiationVulnerablePatch
    {
        public static void Postfix(
            RadiationVulnerable.StatesInstance __instance, ref int __result)
        {
            if (SpacePlantingBrick.FindForPlant(__instance.master.gameObject) != null)
            {
                __result = 0;
            }
        }
    }
}
