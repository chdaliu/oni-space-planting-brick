using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace SpacePlantingBrick
{
    /// <summary>
    /// Core behaviour of the Space Planting Brick.
    ///
    /// Crops planted in the brick are kept at the midpoint of their comfortable
    /// temperature range.
    ///
    /// Resources are not supplied for free: liquid irrigation must arrive
    /// through the brick's liquid pipe, and solid fertilizer must be delivered
    /// by duplicants. Crops whose irrigation is not a liquid (for example
    /// Chlorine for Gas Grass) fall back to manual delivery instead.
    ///
    /// Light: only crops with an IlluminationVulnerable component that does not
    /// prefer darkness use the integrated grow light.
    ///
    /// Environment: the brick cell and every non-solid cell occupied by the
    /// crop can be fully sealed (no gas, liquid, solid, or heat exchange),
    /// gas-permeable only, or gas+liquid permeable. Regardless of the chosen
    /// mode, the brick maintains the crop's pressure, temperature, light, and
    /// radiation conditions, so the crop is unaffected by the external
    /// atmosphere or light.
    /// </summary>
    /// <summary>
    /// Environment levels for the brick's plant chamber.
    /// </summary>
    public enum SpacePlantingEnvironmentMode
    {
        Sealed = 0,
        GasOnly = 1,
        GasLiquid = 2,
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class SpacePlantingBrick : KMonoBehaviour, ISim200ms, IGameObjectEffectDescriptor
    {
        // GasImpermeable/LiquidImpermeable are checked by the sim when it tries
        // to move a gas or liquid packet into the cell. SolidImpermeable does
        // the same for falling solids. The tile cell already has
        // SolidImpermeable from SimCellOccupier, so solid cells only manage the
        // gas/liquid flags.
        private const byte GasImpermeable =
            (byte)Sim.Cell.Properties.GasImpermeable;
        private const byte LiquidImpermeable =
            (byte)Sim.Cell.Properties.LiquidImpermeable;
        private const byte SolidImpermeable =
            (byte)Sim.Cell.Properties.SolidImpermeable;

        private const byte AllCellProperties =
            (byte)(GasImpermeable | LiquidImpermeable | SolidImpermeable);

        // The sim stores insulation as a byte where 0 is perfect insulation.
        // SimMessages.SetInsulation(1f) restores the default conductivity.
        private const float SealedInsulation = 0f;
        private const float DefaultInsulation = 1f;

        private const float RefreshInterval = 0.5f;

        [Serialize]
        public bool sealedMode = true;

        // Kept separate from sealedMode so old saves that only stored
        // sealedMode=false keep their previous gas+liquid permeable behaviour.
        [Serialize]
        public bool liquidPermeable = true;

        private PlantablePlot plot;
        private Storage storage;
        private Light2D growLight;
        private Rotatable rotatable;
        private GameObject plant;

        private float refreshTimer = RefreshInterval;
        private float maintainTimer = RefreshInterval;

        private readonly HashSet<int> managedCells = new HashSet<int>();
        private readonly List<int> desiredCells = new List<int>();
        private readonly List<int> releaseCells = new List<int>();
        private readonly List<Tag> supplyTags = new List<Tag>();

        private float targetTemperature = 293.15f;
        private bool needsLight;
        private bool plantConfigured;
        private Orientation lastOrientation = Orientation.Neutral;

        // Rotatable.OrientVisualizer triggers this hash after the building is
        // flipped, including the flip applied while a building is placed.
        private static readonly EventSystem.IntraObjectHandler<SpacePlantingBrick> OnRotatedDelegate =
            new EventSystem.IntraObjectHandler<SpacePlantingBrick>(
                delegate (SpacePlantingBrick component, object data)
                {
                    component.OnRotated(data);
                });

        private static readonly EventSystem.IntraObjectHandler<SpacePlantingBrick> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<SpacePlantingBrick>(
                delegate (SpacePlantingBrick component, object data)
                {
                    component.OnCopySettings(data);
                });

        public SpacePlantingEnvironmentMode EnvironmentMode
        {
            get
            {
                if (sealedMode)
                {
                    return SpacePlantingEnvironmentMode.Sealed;
                }
                return liquidPermeable
                    ? SpacePlantingEnvironmentMode.GasLiquid
                    : SpacePlantingEnvironmentMode.GasOnly;
            }
        }

        public GameObject PlantedObject
        {
            get { return plant; }
        }

        private Orientation CurrentOrientation
        {
            get { return rotatable != null ? rotatable.GetOrientation() : Orientation.Neutral; }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe(-1643076535, OnRotatedDelegate);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        private void OnRotated(object data)
        {
            Orientation orientation = CurrentOrientation;
            if (orientation == lastOrientation)
            {
                return;
            }

            lastOrientation = orientation;
            UpdateLightOffset();
            ApplyEnvironment();
        }

        private void OnCopySettings(object data)
        {
            GameObject source = data as GameObject;
            if (source == null)
            {
                return;
            }

            SpacePlantingBrick other = source.GetComponent<SpacePlantingBrick>();
            if (other == null)
            {
                return;
            }

            sealedMode = other.sealedMode;
            liquidPermeable = other.liquidPermeable;
            ApplyEnvironment();
            RefreshUserMenu();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            plot = GetComponent<PlantablePlot>();
            storage = GetComponent<Storage>();
            growLight = GetComponent<Light2D>();
            rotatable = GetComponent<Rotatable>();

            if (growLight != null)
            {
                growLight.enabled = false;
            }

            refreshTimer = RefreshInterval;
            maintainTimer = RefreshInterval;

            // Restore the saved plant association, grow-light side, and
            // environment immediately; the periodic refresh is only a safety net.
            lastOrientation = CurrentOrientation;
            UpdateLightOffset();
            RefreshPlant();
            ApplyEnvironment();
        }

        protected override void OnCleanUp()
        {
            ClearManagedCells();

            if (growLight != null)
            {
                growLight.enabled = false;
            }

            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            refreshTimer -= dt;
            if (refreshTimer <= 0f)
            {
                refreshTimer = RefreshInterval;
                RefreshPlant();
            }

            maintainTimer -= dt;
            if (maintainTimer <= 0f)
            {
                maintainTimer = RefreshInterval;
                Maintain();
            }
        }

        public void CycleEnvironmentMode()
        {
            switch (EnvironmentMode)
            {
                case SpacePlantingEnvironmentMode.Sealed:
                    sealedMode = false;
                    liquidPermeable = false;
                    break;
                case SpacePlantingEnvironmentMode.GasOnly:
                    liquidPermeable = true;
                    break;
                default:
                    sealedMode = true;
                    liquidPermeable = true;
                    break;
            }

            ApplyEnvironment();
            RefreshUserMenu();
        }

        private void UpdateLightOffset()
        {
            if (growLight != null)
            {
                Orientation orientation = CurrentOrientation;
                bool inverted = orientation == Orientation.FlipV
                    || orientation == Orientation.R180;
                growLight.Offset = inverted
                    ? new Vector2(0f, -1f)
                    : new Vector2(0f, 1f);
            }
        }

        // ------------------------------------------------------------------
        // plant tracking
        // ------------------------------------------------------------------

        private void RefreshPlant()
        {
            GameObject current = (plot != null && plot.plant != null)
                ? plot.plant.gameObject
                : null;

            if (plant == null || current != plant)
            {
                plant = current;
                plantConfigured = false;
                supplyTags.Clear();
                needsLight = false;

                if (growLight != null)
                {
                    growLight.enabled = false;
                }
            }

            if (plant == null)
            {
                return;
            }

            if (!plantConfigured)
            {
                ConfigurePlant();
            }
        }

        private void ConfigurePlant()
        {
            supplyTags.Clear();
            bool pipeIrrigation = false;

            StateMachineController controller = plant.GetComponent<StateMachineController>();
            if (controller != null)
            {
                IrrigationMonitor.Def irrigation = controller.GetDef<IrrigationMonitor.Def>();
                if (irrigation != null && irrigation.consumedElements != null
                    && irrigation.consumedElements.Length > 0)
                {
                    // The brick has a liquid pipe, so only liquid irrigation can
                    // use the pipe. Non-liquid irrigation (such as Chlorine for
                    // Gas Grass) must keep manual delivery available.
                    pipeIrrigation = true;
                    for (int i = 0; i < irrigation.consumedElements.Length; i++)
                    {
                        Tag tag = irrigation.consumedElements[i].tag;
                        AddSupplyTag(tag);

                        Element element = ElementLoader.FindElementByTag(tag);
                        if (element == null || !element.IsLiquid)
                        {
                            pipeIrrigation = false;
                        }
                    }
                }

                FertilizationMonitor.Def fertilization = controller.GetDef<FertilizationMonitor.Def>();
                if (fertilization != null && fertilization.consumedElements != null)
                {
                    for (int i = 0; i < fertilization.consumedElements.Length; i++)
                    {
                        AddSupplyTag(fertilization.consumedElements[i].tag);
                    }
                }
            }

            ApplySupplyMethod(pipeIrrigation);

            TemperatureVulnerable temperature = plant.GetComponent<TemperatureVulnerable>();
            if (temperature != null)
            {
                float low = temperature.TemperatureWarningLow;
                float high = temperature.TemperatureWarningHigh;
                if (low > 0f && high > low)
                {
                    targetTemperature = (low + high) * 0.5f;
                }
                else
                {
                    targetTemperature = 293.15f;
                }
            }
            else
            {
                targetTemperature = 293.15f;
            }

            IlluminationVulnerable illumination = plant.GetComponent<IlluminationVulnerable>();
            needsLight = illumination != null && !illumination.prefersDarkness;

            plantConfigured = true;
        }

        private void ApplySupplyMethod(bool pipeIrrigation)
        {
            if (plot != null)
            {
                // With liquid_piping=true the vanilla irrigation monitor
                // disables the plant's manual liquid delivery, so liquid
                // irrigation must come from the pipe. Crops with non-liquid
                // irrigation keep manual delivery enabled.
                plot.SetFertilizationFlags(
                    fertilizer: true, liquid_piping: pipeIrrigation);
            }

            RequireInputs requireInputs = GetComponent<RequireInputs>();
            if (requireInputs != null)
            {
                requireInputs.RequiresInputConduit = pipeIrrigation;
            }

            // Re-apply the storage to the plant's irrigation/fertilizer
            // monitors so their ManualDeliveryKG components pick up the new
            // pipe/manual setting. This is the same event PlantablePlot uses
            // when a plant is registered.
            if (plant != null && storage != null)
            {
                plant.Trigger((int)GameHashes.PlanterStorage, storage);
            }
        }

        private void AddSupplyTag(Tag tag)
        {
            if (tag.IsValid && !supplyTags.Contains(tag))
            {
                supplyTags.Add(tag);
            }
        }

        // ------------------------------------------------------------------
        // per-tick maintenance
        // ------------------------------------------------------------------

        private void Maintain()
        {
            ApplyEnvironment();

            if (plant == null)
            {
                if (growLight != null && growLight.enabled)
                {
                    growLight.enabled = false;
                }
                return;
            }

            PrimaryElement primary = plant.GetComponent<PrimaryElement>();
            if (primary != null)
            {
                if (Mathf.Abs(primary.Temperature - targetTemperature) > 0.01f)
                {
                    primary.Temperature = targetTemperature;
                }
            }

            bool lightOn = needsLight;
            if (growLight != null && growLight.enabled != lightOn)
            {
                growLight.enabled = lightOn;
            }
        }

        // ------------------------------------------------------------------
        // cell sealing
        // ------------------------------------------------------------------

        private void ApplyEnvironment()
        {
            desiredCells.Clear();

            int selfCell = Grid.PosToCell(this);
            if (Grid.IsValidCell(selfCell))
            {
                desiredCells.Add(selfCell);
            }

            if (plant != null)
            {
                OccupyArea area = plant.GetComponent<OccupyArea>();
                if (area != null)
                {
                    CellOffset[] offsets = area.OccupiedCellsOffsets;
                    if (offsets != null)
                    {
                        int baseCell = Grid.PosToCell(plant);
                        for (int i = 0; i < offsets.Length; i++)
                        {
                            int cell = Grid.OffsetCell(baseCell, offsets[i]);
                            if (!Grid.IsValidCell(cell) || Grid.Solid[cell])
                            {
                                continue;
                            }
                            if (!desiredCells.Contains(cell))
                            {
                                desiredCells.Add(cell);
                            }
                        }
                    }
                }
            }

            SpacePlantingEnvironmentMode mode = EnvironmentMode;
            for (int i = 0; i < desiredCells.Count; i++)
            {
                int cell = desiredCells[i];
                ApplyCellEnvironment(cell, mode);
                managedCells.Add(cell);
            }

            releaseCells.Clear();
            foreach (int cell in managedCells)
            {
                if (!desiredCells.Contains(cell))
                {
                    releaseCells.Add(cell);
                }
            }

            for (int i = 0; i < releaseCells.Count; i++)
            {
                int cell = releaseCells[i];
                ClearCellEnvironment(cell);
                managedCells.Remove(cell);
            }
        }

        private static void ApplyCellEnvironment(
            int cell, SpacePlantingEnvironmentMode mode)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            // The tile cell itself is solid, so never touch its
            // SolidImpermeable flag; only the plant's non-solid cells need it.
            byte managed = Grid.Solid[cell]
                ? (byte)(GasImpermeable | LiquidImpermeable)
                : AllCellProperties;

            byte desired;
            switch (mode)
            {
                case SpacePlantingEnvironmentMode.Sealed:
                    desired = managed;
                    break;
                case SpacePlantingEnvironmentMode.GasOnly:
                    // Gas and heat pass, but liquid and falling solids do not.
                    desired = Grid.Solid[cell]
                        ? LiquidImpermeable
                        : (byte)(LiquidImpermeable | SolidImpermeable);
                    break;
                default:
                    // Gas, liquid, and heat all pass normally.
                    desired = 0;
                    break;
            }
            desired = (byte)(desired & managed);

            byte current = Grid.Properties[cell];
            byte toSet = (byte)(desired & ~current);
            byte toClear = (byte)(managed & ~desired & current);
            if (toSet != 0)
            {
                SimMessages.SetCellProperties(cell, (byte)(current | toSet));
            }
            if (toClear != 0)
            {
                SimMessages.ClearCellProperties(cell, toClear);
            }

            if (mode == SpacePlantingEnvironmentMode.Sealed)
            {
                // Zero insulation stops conductive heat transfer between this
                // cell and its neighbours. The plant's own temperature is
                // maintained separately.
                if (Grid.Insulation[cell] != SealedInsulation)
                {
                    SimMessages.SetInsulation(cell, SealedInsulation);
                }
            }
            else if (Grid.Insulation[cell] == SealedInsulation)
            {
                SimMessages.SetInsulation(cell, DefaultInsulation);
            }
        }

        private static void ClearCellEnvironment(int cell)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            byte managed = Grid.Solid[cell]
                ? (byte)(GasImpermeable | LiquidImpermeable)
                : AllCellProperties;
            byte toClear = (byte)(managed & Grid.Properties[cell]);
            if (toClear != 0)
            {
                SimMessages.ClearCellProperties(cell, toClear);
            }

            if (Grid.Insulation[cell] == SealedInsulation)
            {
                SimMessages.SetInsulation(cell, DefaultInsulation);
            }
        }

        private void ClearManagedCells()
        {
            releaseCells.Clear();
            releaseCells.AddRange(managedCells);
            for (int i = 0; i < releaseCells.Count; i++)
            {
                ClearCellEnvironment(releaseCells[i]);
            }
            managedCells.Clear();
            releaseCells.Clear();
        }

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------

        private void RefreshUserMenu()
        {
            if (Game.Instance != null && Game.Instance.userMenu != null)
            {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }

        public static SpacePlantingBrick FindForPlant(GameObject plantObject)
        {
            if (plantObject == null)
            {
                return null;
            }

            int cell = Grid.PosToCell(plantObject);
            if (!Grid.IsValidCell(cell))
            {
                return null;
            }

            // The brick normally sits one cell above an inverted crop or one
            // cell below an upright crop. Larger plants can anchor a few cells
            // away, so check a small radius in both directions.
            SpacePlantingBrick brick = FindForPlantInCell(cell, plantObject);
            for (int distance = 1; brick == null && distance <= 3; distance++)
            {
                brick = FindForPlantInCell(
                    Grid.OffsetCell(cell, 0, -distance), plantObject);
                if (brick == null)
                {
                    brick = FindForPlantInCell(
                        Grid.OffsetCell(cell, 0, distance), plantObject);
                }
            }
            return brick;
        }

        private static SpacePlantingBrick FindForPlantInCell(int cell, GameObject plantObject)
        {
            if (!Grid.IsValidCell(cell))
            {
                return null;
            }

            // Foundation tiles are registered under FoundationTile as well
            // as Building, so check both layers.
            SpacePlantingBrick brick = FindForPlantInLayer(
                cell, ObjectLayer.Building, plantObject);
            if (brick == null)
            {
                brick = FindForPlantInLayer(
                    cell, ObjectLayer.FoundationTile, plantObject);
            }
            return brick;
        }

        private static SpacePlantingBrick FindForPlantInLayer(
            int cell, ObjectLayer layer, GameObject plantObject)
        {
            GameObject building = Grid.Objects[cell, (int)layer];
            SpacePlantingBrick brick = building != null
                ? building.GetComponent<SpacePlantingBrick>()
                : null;
            return brick != null && brick.PlantedObject == plantObject
                ? brick
                : null;
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> descriptors = new List<Descriptor>();

            string mode;
            switch (EnvironmentMode)
            {
                case SpacePlantingEnvironmentMode.Sealed:
                    mode = STRINGS.UI.SPACEPLANTINGBRICK.SEALED;
                    break;
                case SpacePlantingEnvironmentMode.GasOnly:
                    mode = STRINGS.UI.SPACEPLANTINGBRICK.GAS_PERMEABLE;
                    break;
                default:
                    mode = STRINGS.UI.SPACEPLANTINGBRICK.PERMEABLE;
                    break;
            }
            descriptors.Add(new Descriptor(
                mode,
                STRINGS.UI.SPACEPLANTINGBRICK.ENV_DESCRIPTOR_TOOLTIP));

            if (plant == null)
            {
                descriptors.Add(new Descriptor(
                    STRINGS.UI.SPACEPLANTINGBRICK.NO_PLANT,
                    STRINGS.UI.SPACEPLANTINGBRICK.NO_PLANT));
                return descriptors;
            }

            descriptors.Add(new Descriptor(
                string.Format(STRINGS.UI.SPACEPLANTINGBRICK.TARGET_TEMP,
                    GameUtil.GetFormattedTemperature(targetTemperature)),
                STRINGS.UI.SPACEPLANTINGBRICK.TARGET_TEMP_TOOLTIP));

            if (supplyTags.Count > 0)
            {
                List<string> names = new List<string>();
                for (int i = 0; i < supplyTags.Count; i++)
                {
                    names.Add(supplyTags[i].ProperName());
                }

                descriptors.Add(new Descriptor(
                    string.Format(STRINGS.UI.SPACEPLANTINGBRICK.NUTRIENTS,
                        string.Join(", ", names.ToArray())),
                    STRINGS.UI.SPACEPLANTINGBRICK.NUTRIENTS_TOOLTIP));
            }

            descriptors.Add(new Descriptor(
                needsLight
                    ? STRINGS.UI.SPACEPLANTINGBRICK.GROW_LIGHT_ON
                    : STRINGS.UI.SPACEPLANTINGBRICK.GROW_LIGHT_OFF,
                STRINGS.UI.SPACEPLANTINGBRICK.GROW_LIGHT_TOOLTIP));

            return descriptors;
        }
    }
}
