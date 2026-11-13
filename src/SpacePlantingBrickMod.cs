using HarmonyLib;
using KMod;

namespace SpacePlantingBrick
{
    public sealed class SpacePlantingBrickMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }
    }

    /// <summary>
    /// Registers localization, the build-menu entry, and the technology unlock.
    ///
    /// Run from the prefix of GeneratedBuildings.LoadGeneratedBuildings: Db is
    /// already initialized at this point, but the building defs (and their
    /// TechItem post-processing) have not been created yet. Adding the ID to
    /// Agriculture here therefore makes BuildingDef.PostProcess create the
    /// TechItem in the same tech as the vanilla Hydroponic Farm.
    /// </summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildingsPatch
    {
        private static bool initialized;

        public static void Prefix()
        {
            if (!initialized)
            {
                initialized = true;
                STRINGS.Initialize();
                LocString.CreateLocStringKeys(typeof(STRINGS), null);

                ModUtil.AddBuildingToPlanScreen(
                    "Food",
                    SpacePlantingBrickConfig.ID,
                    "farming",
                    "FarmTile",
                    ModUtil.BuildingOrdering.After);
                TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[SpacePlantingBrickConfig.ID] = "farming";
            }

            // Keep the tech unlock idempotent; if the database is rebuilt the
            // building must be re-added before its BuildingDef is post-processed.
            Tech farmTech = Db.Get().Techs != null
                ? Db.Get().Techs.TryGet("Agriculture")
                : null;
            if (farmTech != null && !farmTech.unlockedItemIDs.Contains(SpacePlantingBrickConfig.ID))
            {
                farmTech.AddUnlockedItemIDs(SpacePlantingBrickConfig.ID);
            }
        }
    }
}
