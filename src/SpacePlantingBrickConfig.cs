using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace SpacePlantingBrick
{
    /// <summary>
    /// 1x1 Space Planting Brick. Structurally it is a foundation farm tile: the
    /// brick cell is solid and the crop grows in the adjacent cell above or
    /// below it. The plant plot/storage behaviour mirrors the vanilla Farm
    /// Tile, including its FlipV orientation for hanging crops. Liquids are
    /// supplied through a liquid pipe like the vanilla Hydroponic Farm, solid
    /// fertilizer is delivered by duplicants, and crops that use a gas as
    /// irrigation can use the optional secondary gas pipe. The
    /// SpacePlantingBrick component adds temperature, light, and environment
    /// control, but no longer supplies resources for free.
    /// </summary>
    public sealed class SpacePlantingBrickConfig : IBuildingConfig
    {
        public const string ID = "SpacePlantingBrick";

        private const string ANIM = "space_planting_brick_kanim";

        private static void AddGasInputPort(GameObject go)
        {
            ConduitSecondaryInput port = go.AddOrGet<ConduitSecondaryInput>();
            port.portInfo = new ConduitPortInfo(
                ConduitType.Gas, SpacePlantingGasInput.PortOffset);
        }

        public override BuildingDef CreateBuildingDef()
        {
            // 100 kg of farmable dirt plus 50 kg of metal. The combined
            // "Metal&RefinedMetal" category accepts both metal ore and
            // refined metal.
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                ANIM,
                100,
                30f,
                new float[] { 100f, 50f },
                new string[] { "Farmable", "Metal&RefinedMetal" },
                1600f,
                BuildLocationRule.Tile,
                TUNING.BUILDINGS.DECOR.NONE,
                TUNING.NOISE_POLLUTION.NONE);

            BuildingTemplates.CreateFoundationTileDef(def);
            def.Floodable = false;
            def.Entombable = false;
            def.Overheatable = false;
            def.Repairable = false;
            def.ForegroundLayer = Grid.SceneLayer.BuildingBack;
            def.AudioCategory = "HollowMetal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ConstructionOffsetFilter = BuildingDef.ConstructionOffsetFilter_OneDown;
            // FlipV matches the vanilla Farm Tile: the brick can be built
            // upright (crop above) or inverted (crop below) for hanging crops
            // such as Pincha Pepper.
            def.PermittedRotations = PermittedRotations.FlipV;
            def.DefaultAnimState = "idle";
            def.DragBuild = true;
            def.ShowInBuildMenu = true;
            // Irrigation must be piped in, matching the vanilla Hydroponic
            // Farm. The pipe enters through the brick cell itself.
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.FOOD);
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.FARM);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            KPrefabID prefabId = go.GetComponent<KPrefabID>();
            prefabId.AddTag(GameTags.CodexCategories.FarmBuilding);
            // Do not call MakeBuildingAlwaysOperational: it removes the
            // Operational component that ConduitConsumer requires. A liquid
            // pipe input makes this building operational like the vanilla
            // Hydroponic Farm.
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(
                typeof(RequiresFoundation), prefab_tag);

            SimCellOccupier occupier = go.AddOrGet<SimCellOccupier>();
            occupier.doReplaceElement = true;
            occupier.notifyOnMelt = true;

            go.AddOrGet<TileTemperature>();

            ConduitConsumer consumer = go.AddOrGet<ConduitConsumer>();
            consumer.conduitType = ConduitType.Liquid;
            consumer.consumptionRate = 1f;
            consumer.capacityKG = 5f;
            consumer.capacityTag = GameTags.Liquid;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;

            Storage storage = BuildingTemplates.CreateDefaultStorage(go);
            storage.capacityKg = 500f;
            storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);

            PlantablePlot plot = go.AddOrGet<PlantablePlot>();
            plot.occupyingObjectRelativePosition = new Vector3(0f, 1f, 0f);
            plot.AddDepositTag(GameTags.CropSeed);
            plot.AddDepositTag(GameTags.WaterSeed);
            plot.AddAdditionalCriteria(FarmTileConfig.ForbiddenTags);

            // liquid_piping=true suppresses manual irrigation delivery, so
            // liquid crops must be connected to the liquid pipe. Fertilizer is
            // still delivered by duplicants. SpacePlantingBrick toggles this
            // flag off for crops whose irrigation is not a liquid.
            plot.SetFertilizationFlags(fertilizer: true, liquid_piping: true);

            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Farm;

            Prioritizable.AddRef(go);

            // Optional secondary gas input for crops that use a gas as
            // irrigation, such as Gas Grass with Chlorine.
            AddGasInputPort(go);
            go.AddOrGet<SpacePlantingGasInput>();

            // Vanilla Hydroponic Farm behaviour: let duplicants empty the
            // internal storage, e.g. after a wrong liquid was piped in.
            DropAllWorkable dropAll = go.AddOrGet<DropAllWorkable>();
            dropAll.removeTags = new List<Tag>();

            go.AddOrGet<SpacePlantingBrick>();
            go.AddOrGet<SpacePlantingBrickEnvironmentButton>();

            Light2D light = go.AddOrGet<Light2D>();
            light.Color = new Color(1f, 0.93f, 0.78f, 1f);
            light.Range = 2f;
            light.Lux = 20000;
            light.FalloffRate = 0.4f;
            light.shape = LightShape.Circle;
            light.Offset = new Vector2(0f, 1f);
            light.drawOverlay = false;
            light.enabled = false;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AddGasInputPort(go);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            AddGasInputPort(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<KPrefabID>().AddTag(GameTags.FarmTiles);
            go.GetComponent<KBatchedAnimController>().initialBlendParameters = 4;
            // An empty but connected pipe must not shut the building down.
            // The crop's own irrigation state reports the missing liquid.
            go.GetComponent<RequireInputs>().requireConduitHasMass = false;
        }
    }
}
