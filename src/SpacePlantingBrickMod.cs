using HarmonyLib;
using KMod;

namespace SpacePlantingBrick
{
    public sealed class SpacePlantingBrickMod : UserMod2
    {
        // The raw mod.yaml title and description. ModsScreen calls
        // Strings.TryGet on both, so registering these exact strings as keys
        // lets the game replace them with the current language's text.
        private static string titleKey;
        private static string descriptionKey;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            titleKey = mod != null ? mod.title : null;
            descriptionKey = mod != null ? mod.description : null;
        }

        public static void RegisterLocalizedModInfo()
        {
            LocString title = STRINGS.BUILDINGS.PREFABS.SPACEPLANTINGBRICK.NAME;
            if (!string.IsNullOrEmpty(titleKey) && title != null)
            {
                Strings.Add(titleKey, title.text);
            }
            if (!string.IsNullOrEmpty(descriptionKey)
                && STRINGS.MOD_INFO.DESCRIPTION != null)
            {
                Strings.Add(descriptionKey, STRINGS.MOD_INFO.DESCRIPTION.text);
            }
        }
    }

    /// <summary>
    /// Localization.Initialize runs after the game has selected and loaded the
    /// current language, and before the Mods screen is built. Initialize our
    /// strings there so the mod list tooltip and all building text are ready
    /// as soon as the game needs them.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
    public static class LocalizationInitializePatch
    {
        public static void Postfix()
        {
            STRINGS.Initialize();
            LocString.CreateLocStringKeys(typeof(STRINGS), null);
            SpacePlantingBrickMod.RegisterLocalizedModInfo();
        }
    }

    /// <summary>
    /// Registers the build-menu entry and the technology unlock.
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

                // Defensive fallback for the unlikely case that the mod was
                // loaded after Localization.Initialize already ran.
                if (STRINGS.BUILDINGS.PREFABS.SPACEPLANTINGBRICK.NAME == null)
                {
                    STRINGS.Initialize();
                    LocString.CreateLocStringKeys(typeof(STRINGS), null);
                    SpacePlantingBrickMod.RegisterLocalizedModInfo();
                }

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
