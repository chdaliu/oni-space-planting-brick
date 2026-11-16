using System.Collections.Generic;
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
    /// GetPressureOverArea is where PressureVulnerable reads the real cell
    /// pressure and decides whether the atmosphere is safe. Overriding the
    /// result here makes that original logic see a safe area, so it never
    /// fires a "wrong atmosphere / wrong pressure" transition. Overriding the
    /// state machine values after SlicedSim1000ms instead would flip the
    /// plant between unsafe and safe every second in a vacuum cell, which
    /// repeatedly cancels the wilt-recovery timer and leaves the crop stuck
    /// in the wilting state.
    /// </summary>
    [HarmonyPatch(typeof(PressureVulnerable), "GetPressureOverArea")]
    public static class PressureVulnerableAreaPatch
    {
        private const float PreferredInternalPressure = 2f;

        public static void Postfix(
            PressureVulnerable __instance, ref float __result)
        {
            if (SpacePlantingBrick.FindForPlant(__instance.gameObject) == null)
            {
                return;
            }

            __instance.testAreaElementSafe = true;

            float pressure = PreferredInternalPressure;
            float low = __instance.pressureWarning_Low;
            float high = __instance.pressureWarning_High;
            if (high > low)
            {
                pressure = Mathf.Clamp(pressure, low + 0.01f, high - 0.01f);
            }

            __result = pressure;
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

    /// <summary>
    /// DigTool.PlaceDig refuses to place a dig marker in a cell when any
    /// object in any object layer has a Constructable. An under-construction
    /// building also registers its conduit ports in the conduit layer of the
    /// port cell, even when the port is outside the building footprint. The
    /// brick's gas port sits in the crop cell at (0, 1), so while the brick is
    /// being built that cell looks occupied to the dig tool and cannot be
    /// marked for digging.
    ///
    /// The port does not actually occupy the cell, so temporarily hide such
    /// non-occupying Constructables while PlaceDig runs. This fixes digging
    /// the crop cell of a brick that is queued under a natural tile.
    /// </summary>
    [HarmonyPatch(typeof(DigTool), nameof(DigTool.PlaceDig))]
    public static class DigToolPlaceDigPatch
    {
        private static readonly List<KeyValuePair<int, GameObject>> hiddenPorts =
            new List<KeyValuePair<int, GameObject>>();

        public static void Prefix(int cell)
        {
            hiddenPorts.Clear();
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            for (int layer = 0; layer < (int)ObjectLayer.NumLayers; layer++)
            {
                GameObject go = Grid.Objects[cell, layer];
                if (go == null || go.GetComponent<Constructable>() == null)
                {
                    continue;
                }

                // Only hide constructions whose footprint does not include
                // this cell, i.e. an out-of-footprint conduit port.
                Building building = go.GetComponent<Building>();
                if (building == null || building.PlacementCellsContainCell(cell))
                {
                    continue;
                }

                hiddenPorts.Add(new KeyValuePair<int, GameObject>(layer, go));
            }

            for (int i = 0; i < hiddenPorts.Count; i++)
            {
                Grid.Objects[cell, hiddenPorts[i].Key] = null;
            }
        }

        public static void Postfix(int cell)
        {
            for (int i = 0; i < hiddenPorts.Count; i++)
            {
                Grid.Objects[cell, hiddenPorts[i].Key] = hiddenPorts[i].Value;
            }
            hiddenPorts.Clear();
        }
    }
}
