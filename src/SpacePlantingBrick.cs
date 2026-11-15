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
    /// Chlorine for Gas Grass) fall back to manual delivery instead. The pipe
    /// input is filtered to the crop's required irrigation resource so a
    /// wrong liquid or gas cannot fill the input buffer.
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
        private Building building;
        private GameObject plant;

        // Pipe input filtering. The brick's pipe should only take the
        // irrigation resource the planted crop actually uses; otherwise a
        // wrong liquid/gas fills the 5 kg input buffer and the correct
        // resource can never enter.
        private ConduitConsumer liquidConsumer;
        private ConduitConsumer gasConsumer;
        private bool pendingIrrigationDump;

        private float refreshTimer = RefreshInterval;
        private float maintainTimer = RefreshInterval;

        private readonly HashSet<int> managedCells = new HashSet<int>();
        private readonly List<int> desiredCells = new List<int>();
        private readonly List<int> releaseCells = new List<int>();
        private readonly List<Tag> supplyTags = new List<Tag>();
        private readonly List<Tag> requiredLiquidTags = new List<Tag>();
        private readonly List<Tag> requiredGasTags = new List<Tag>();

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
            building = GetComponent<Building>();

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

            // Keep the pipe filters in sync every sim tick so a wrong
            // liquid/gas is never pumped into the brick for long.
            UpdateConduitFilters();
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
                requiredLiquidTags.Clear();
                requiredGasTags.Clear();
                pendingIrrigationDump = false;
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
            requiredLiquidTags.Clear();
            requiredGasTags.Clear();
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
                        if (element != null && element.IsLiquid)
                        {
                            if (!requiredLiquidTags.Contains(tag))
                            {
                                requiredLiquidTags.Add(tag);
                            }
                        }
                        else if (element != null && element.IsGas)
                        {
                            if (!requiredGasTags.Contains(tag))
                            {
                                requiredGasTags.Add(tag);
                            }
                        }

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

            // Drop any irrigation resource left over from a previous crop or
            // from the old "accept any liquid" behaviour once the new crop's
            // requirements are known.
            pendingIrrigationDump = true;
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

            UpdateConduitFilters();
        }

        // ------------------------------------------------------------------
        // pipe input filtering
        // ------------------------------------------------------------------

        private void UpdateConduitFilters()
        {
            if (Game.Instance == null)
            {
                return;
            }

            UpdateConduitConsumer(
                GetLiquidConsumer(),
                ConduitType.Liquid,
                GetLiquidInputCell(),
                requiredLiquidTags);

            UpdateConduitConsumer(
                GetGasConsumer(),
                ConduitType.Gas,
                GetGasInputCell(),
                requiredGasTags);

            if (pendingIrrigationDump && plant != null && storage != null)
            {
                pendingIrrigationDump = false;

                // Removes irrigation liquid left over from a previous crop or
                // from a wrong liquid that was piped in before this crop was
                // configured. IrrigationMonitor already knows which elements
                // the current crop accepts.
                IrrigationMonitor.Instance.DumpIncorrectFertilizers(
                    storage, plant);
            }
        }

        private void UpdateConduitConsumer(
            ConduitConsumer consumer,
            ConduitType conduitType,
            int inputCell,
            List<Tag> acceptedTags)
        {
            if (consumer == null)
            {
                return;
            }

            bool hasAccepted = acceptedTags.Count > 0;
            bool pipeMatches = false;

            if (hasAccepted && Grid.IsValidCell(inputCell))
            {
                ConduitFlow flow = conduitType == ConduitType.Gas
                    ? Game.Instance.gasConduitFlow
                    : Game.Instance.liquidConduitFlow;
                if (flow != null)
                {
                    ConduitFlow.ConduitContents contents =
                        flow.GetContents(inputCell);

                    // An empty pipe is fine; the consumer may wait for the
                    // correct resource to arrive.
                    if (contents.mass <= 0f)
                    {
                        pipeMatches = true;
                    }
                    else
                    {
                        Element element =
                            ElementLoader.FindElementByHash(contents.element);
                        pipeMatches = element != null
                            && ElementMatchesAny(element, acceptedTags);
                    }
                }
            }

            // Only consume while the pipe carries an accepted resource. This
            // keeps wrong liquids/gases in the pipe instead of eating them or
            // filling the brick's input buffer.
            consumer.SetOnState(hasAccepted && pipeMatches);
            consumer.capacityTag = hasAccepted && acceptedTags.Count == 1
                ? acceptedTags[0]
                : (conduitType == ConduitType.Gas
                    ? GameTags.Gas
                    : GameTags.Liquid);
        }

        private static bool ElementMatchesAny(Element element, List<Tag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (element.HasTag(tags[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private ConduitConsumer GetLiquidConsumer()
        {
            if (liquidConsumer == null)
            {
                ConduitConsumer[] consumers = GetComponents<ConduitConsumer>();
                for (int i = 0; i < consumers.Length; i++)
                {
                    if (consumers[i].conduitType == ConduitType.Liquid
                        && !consumers[i].useSecondaryInput)
                    {
                        liquidConsumer = consumers[i];
                        break;
                    }
                }
            }
            return liquidConsumer;
        }

        private ConduitConsumer GetGasConsumer()
        {
            if (gasConsumer == null)
            {
                SpacePlantingGasInput gasInput =
                    GetComponent<SpacePlantingGasInput>();
                if (gasInput != null)
                {
                    gasConsumer = gasInput.Consumer;
                }

                if (gasConsumer == null)
                {
                    ConduitConsumer[] consumers = GetComponents<ConduitConsumer>();
                    for (int i = 0; i < consumers.Length; i++)
                    {
                        if (consumers[i].useSecondaryInput
                            && consumers[i].conduitType == ConduitType.Gas)
                        {
                            gasConsumer = consumers[i];
                            break;
                        }
                    }
                }
            }
            return gasConsumer;
        }

        private int GetLiquidInputCell()
        {
            if (building == null)
            {
                building = GetComponent<Building>();
            }
            return building != null ? building.GetUtilityInputCell() : -1;
        }

        private int GetGasInputCell()
        {
            if (building == null)
            {
                building = GetComponent<Building>();
            }
            return building != null
                ? Grid.OffsetCell(
                    building.NaturalBuildingCell(),
                    building.GetRotatedOffset(SpacePlantingGasInput.PortOffset))
                : -1;
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
