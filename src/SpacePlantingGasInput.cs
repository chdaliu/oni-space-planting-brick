namespace SpacePlantingBrick
{
    /// <summary>
    /// Optional secondary gas input for crops that use a gas as irrigation,
    /// for example Gas Grass with Chlorine. The gas is added to the brick's
    /// normal Storage and consumed by the crop's IrrigationMonitor, exactly
    /// like piped liquid irrigation.
    ///
    /// The port sits in the crop cell so it follows FlipV orientation. This
    /// component is only present on the completed building; the preview and
    /// under-construction objects get the port data from ConduitSecondaryInput.
    /// </summary>
    public sealed class SpacePlantingGasInput : KMonoBehaviour
    {
        // (0, 1) is the crop cell for an upright brick and rotates to (0, -1)
        // for an inverted brick.
        public static readonly CellOffset PortOffset = new CellOffset(0, 1);

        private int inputCell = -1;
        private FlowUtilityNetwork.NetworkItem networkItem;
        private SpacePlantingGasConduitConsumer consumer;

        public ConduitConsumer Consumer
        {
            get { return consumer; }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            Building building = GetComponent<Building>();
            if (building == null)
            {
                return;
            }

            inputCell = Grid.OffsetCell(
                building.NaturalBuildingCell(),
                building.GetRotatedOffset(PortOffset));

            consumer = gameObject.AddComponent<SpacePlantingGasConduitConsumer>();
            consumer.conduitType = ConduitType.Gas;
            consumer.useSecondaryInput = true;
            consumer.consumptionRate = 1f;
            consumer.capacityKG = 5f;
            consumer.capacityTag = GameTags.Gas;
            // SpacePlantingBrick filters the pipe contents and only enables the
            // consumer for the crop's required gas, so a wrong gas should be
            // kept inside the brick rather than dumped onto the ground.
            consumer.wrongElementResult =
                ConduitConsumer.WrongElementResult.Store;

            networkItem = new FlowUtilityNetwork.NetworkItem(
                ConduitType.Gas, Endpoint.Sink, inputCell, gameObject);
            Conduit.GetNetworkManager(ConduitType.Gas).AddToNetworks(
                inputCell, networkItem, is_endpoint: true);
        }

        protected override void OnCleanUp()
        {
            if (networkItem != null)
            {
                Conduit.GetNetworkManager(ConduitType.Gas).RemoveFromNetworks(
                    inputCell, networkItem, is_endpoint: true);
                networkItem = null;
            }

            base.OnCleanUp();
        }
    }

    /// <summary>
    /// The base ConduitConsumer resolves a secondary input from the raw
    /// ConduitSecondaryInput offset, which does not follow the building's
    /// FlipV orientation. This override uses the same rotated cell that the
    /// port and network endpoint use.
    /// </summary>
    [SkipSaveFileSerialization]
    public sealed class SpacePlantingGasConduitConsumer : ConduitConsumer
    {
        protected override int GetInputCell(ConduitType inputConduitType)
        {
            return Grid.OffsetCell(
                building.NaturalBuildingCell(),
                building.GetRotatedOffset(SpacePlantingGasInput.PortOffset));
        }
    }
}
