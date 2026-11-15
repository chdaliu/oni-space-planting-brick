namespace SpacePlantingBrick
{
    /// <summary>
    /// Building and UI text. Initialize() picks Chinese or English once the
    /// game's localization system is ready, so no external translation files
    /// are required.
    /// </summary>
    public static class STRINGS
    {
        public static class BUILDINGS
        {
            public static class PREFABS
            {
                public static class SPACEPLANTINGBRICK
                {
                    public static LocString NAME;
                    public static LocString DESC;
                    public static LocString EFFECT;
                    public static LocString FLAVOR;
                }
            }
        }

        /// <summary>
        /// Text shown for the mod itself in the in-game Mods screen.
        /// </summary>
        public static class MOD_INFO
        {
            public static LocString DESCRIPTION;
        }

        public static class UI
        {
            public static class SPACEPLANTINGBRICK
            {
                public static LocString SEALED;
                public static LocString GAS_PERMEABLE;
                public static LocString PERMEABLE;
                public static LocString ENV_TOOLTIP;
                public static LocString ENV_DESCRIPTOR_TOOLTIP;
                public static LocString TARGET_TEMP;
                public static LocString TARGET_TEMP_TOOLTIP;
                public static LocString NUTRIENTS;
                public static LocString NUTRIENTS_TOOLTIP;
                public static LocString GROW_LIGHT_ON;
                public static LocString GROW_LIGHT_OFF;
                public static LocString GROW_LIGHT_TOOLTIP;
                public static LocString NO_PLANT;
            }
        }

        public static void Initialize()
        {
            bool zh = UseChinese();

            BUILDINGS.PREFABS.SPACEPLANTINGBRICK.NAME = L(zh,
                "太空种植砖",
                "Space Planting Brick");
            BUILDINGS.PREFABS.SPACEPLANTINGBRICK.DESC = L(zh,
                "一块自带环境维持系统的种植砖，专为真空、极端温度和远离殖民地的地方设计。\n\n" +
                "砖体会自动把种在其中的植物维持在它最适宜的温度，" +
                "并在需要时点亮内置生长灯。灌溉液需要通过液体管道输入砖体，" +
                "以气体为灌溉物的作物还可以接入气体管道，固体肥料则由复制人搬运；" +
                "种植砖不会免费提供资源。" +
                "砖体内部是类似液培砖的营养液槽与生长介质，资源会储存在槽内供植物吸收。\n\n" +
                "通过建筑详情面板下方功能区的按钮，可以在“完全隔绝”“仅透气”“透气透水”之间循环切换。" +
                "完全隔绝时，植物所在格不再与外界交换热量、气体和液体；" +
                "仅透气时，气体和热量可以进出植物所在格，但液体不能进入；" +
                "透气透水时，气体、液体和热量都可以正常进出。" +
                "无论选择哪种模式，种植砖都会维持植物所需的气压、温度、光照和辐射条件，" +
                "植物不受外界环境影响；但灌溉和肥料仍需按上述方式供应。\n\n" +
                "和原版液培砖一样支持上下翻转：正立时植物向上生长，倒置时植物向下生长，" +
                "适合火椒等垂吊植物。贴图尺寸和位置也与原版液培砖保持一致。\n\n" +
                "使用泥土和金属矿石或精炼金属建造，与原版液培砖处于同一科技。",
                "A planting brick with its own environment-control system, made for vacuum, " +
                "extreme temperatures, and places far from the colony.\n\n" +
                "The brick automatically keeps a planted crop at its ideal temperature and " +
                "turns on its integrated grow light when needed. Irrigation liquid must be " +
                "piped into the brick; crops that use a gas as irrigation can use the optional " +
                "gas pipe, and solid fertilizer must be delivered by duplicants. The brick does " +
                "not supply resources for free. Inside, a hydroponic-style nutrient reservoir " +
                "stores resources for the crop to absorb.\n\n" +
                "The action area below the building details panel cycles through Fully Sealed, " +
                "Gas Permeable, and Gas + Liquid Permeable. When fully sealed, the plant's cells " +
                "exchange no heat, gas, or liquid with the outside. Gas Permeable lets gas and heat " +
                "through but blocks liquid; Gas + Liquid Permeable lets gas, liquid, and heat pass " +
                "normally. In every mode, the brick maintains the crop's pressure, temperature, " +
                "light, and radiation conditions, so the crop is unaffected by the outside " +
                "environment; irrigation and fertilizer still have to be supplied as described.\n\n" +
                "Like the vanilla Hydroponic Farm, the brick can be flipped vertically: upright " +
                "crops grow above it, while inverted crops grow below it, which suits hanging " +
                "plants such as Pincha Pepper. The sprite size and placement match the vanilla " +
                "Hydroponic Farm.\n\n" +
                "Built from dirt plus metal ore or refined metal, and unlocked by the same tech " +
                "as the Hydroponic Farm.");
            BUILDINGS.PREFABS.SPACEPLANTINGBRICK.EFFECT = L(zh,
                "自动维持植物适宜温度\n灌溉液需接入液体管道\n只吸取作物所需灌溉物\n气体灌溉作物可接入气体管道\n固体肥料由复制人搬运\n" +
                "内置营养液槽与生长介质\n" +
                "需要光照时自带生长灯\n可切换完全隔绝 / 仅透气 / 透气透水\n" +
                "植物气压/温度/光照/辐射由砖体维持\n植物不受外界环境影响\n" +
                "可倒置种植火椒等垂吊植物\n尺寸与位置同原版液培砖\n" +
                "泥土 + 金属矿石/精炼金属建造",
                "Keeps crops at their ideal temperature\nLiquid irrigation must be piped in\n" +
                "Only the crop's required irrigation is absorbed\n" +
                "Gas-irrigation crops can use a gas pipe\nSolid fertilizer delivered by duplicants\n" +
                "Hydroponic nutrient reservoir and grow bed\n" +
                "Integrated grow light when needed\nCycle Fully Sealed / Gas Permeable / Gas + Liquid Permeable\n" +
                "Brick maintains crop pressure, temperature, light, and radiation\n" +
                "Crops ignore the outside environment\n" +
                "Invertible for hanging crops like Pincha Pepper\n" +
                "Vanilla Hydroponic Farm size and placement\nBuilt from dirt + metal ore or refined metal");
            BUILDINGS.PREFABS.SPACEPLANTINGBRICK.FLAVOR = L(zh,
                "\"在真空里，每一滴营养液都要从殖民地送上来。\"",
                "\"In vacuum, every drop of nutrient solution has to be shipped from the colony.\"");

            MOD_INFO.DESCRIPTION = L(zh,
                "添加 1x1 的太空种植砖。自动把植物维持在它最适宜的温度，并在需要时点亮内置生长灯。" +
                "灌溉液需要通过液体管道输入砖体；以气体为灌溉物的作物还可接入气体管道；" +
                "固体肥料由复制人搬运，不再免费提供资源。" +
                "在建筑详情面板下方的功能区可在“完全隔绝”“仅透气”“透气透水”三档之间循环切换：" +
                "完全隔绝时，植物所在格不与外界交换热量、气体和液体；" +
                "仅透气时，气体和热量可以通过，液体不能进入；" +
                "透气透水时，气体、液体和热量都可以正常进出。" +
                "无论选择哪种模式，植物所需的气压、温度、光照和辐射条件都由种植砖维持，植物不受外界环境影响；" +
                "灌溉和肥料仍需自行供应，且管道只吸取作物需要的灌溉液/气体。" +
                "砖体贴图取自原版液培砖并改为深空冷色，尺寸和位置也直接沿用原版液培砖（包括动画帧偏移）；" +
                "旋转沿用原版液培砖的 FlipV 逻辑，倒置后植物向下生长，适合火椒等垂吊植物。" +
                "使用泥土与金属矿石/精炼金属建造，与原版液培砖处于同一科技。",
                "Adds a 1x1 Space Planting Brick. It automatically keeps a planted crop at its ideal " +
                "temperature and turns on its integrated grow light when needed. Irrigation liquid must " +
                "be piped into the brick; crops that use a gas as irrigation can use the gas pipe, and " +
                "solid fertilizer must be delivered by duplicants. Resources are no longer supplied for " +
                "free. The action area below the building details panel cycles through Fully Sealed, Gas " +
                "Permeable, and Gas + Liquid Permeable. When sealed, the plant's cells exchange no heat, " +
                "gas, or liquid with the outside. Gas Permeable lets gas and heat through but blocks " +
                "liquid; Gas + Liquid Permeable lets gas, liquid, and heat pass normally. In every mode, " +
                "the brick maintains the crop's pressure, temperature, light, and radiation conditions, " +
                "so the crop is unaffected by the outside environment; irrigation and fertilizer still " +
                "have to be supplied, and the pipes only absorb the irrigation liquid/gas the crop " +
                "actually uses. The brick reuses the vanilla Hydroponic Farm art in a cold " +
                "deep-space palette and keeps the vanilla sprite size, bounds, and animation-frame " +
                "offset. It uses the vanilla Hydroponic Farm's FlipV rotation logic, so inverted bricks " +
                "grow crops downwards, which suits hanging plants such as Pincha Pepper. Built from dirt " +
                "plus metal ore or refined metal, and unlocked by the same tech as the Hydroponic Farm.");

            UI.SPACEPLANTINGBRICK.SEALED = L(zh, "完全隔绝", "Fully Sealed");
            UI.SPACEPLANTINGBRICK.GAS_PERMEABLE = L(zh, "仅透气", "Gas Permeable");
            UI.SPACEPLANTINGBRICK.PERMEABLE = L(zh, "透气透水", "Gas + Liquid Permeable");
            UI.SPACEPLANTINGBRICK.ENV_TOOLTIP = L(zh,
                "无论哪种模式，植物所需的气压、温度、光照和辐射条件都由种植砖维持，不受外界环境影响。\n" +
                "灌溉液需要通过液体管道输入，固体肥料由复制人搬运；种植砖不会免费提供资源。\n" +
                "完全隔绝：植物所在格不与外界交换热量、气体和液体。\n" +
                "仅透气：气体和热量可以进出植物所在格，但液体不能进入。\n" +
                "透气透水：气体、液体和热量都可以正常进出植物所在格。",
                "Regardless of mode, the brick maintains the crop's pressure, temperature, light, " +
                "and radiation conditions, so the crop is unaffected by the outside environment.\n" +
                "Irrigation liquid must be piped in and solid fertilizer must be delivered by " +
                "duplicants; the brick does not supply resources for free.\n" +
                "Fully Sealed: the plant's cells exchange no heat, gas, or liquid with the outside.\n" +
                "Gas Permeable: gas and heat pass through the plant's cells, but liquid cannot enter.\n" +
                "Gas + Liquid Permeable: gas, liquid, and heat pass through the plant's cells normally.");

            UI.SPACEPLANTINGBRICK.ENV_DESCRIPTOR_TOOLTIP = L(zh,
                "当前的环境模式，只影响植物所在格与外界的交换。植物所需的气压、温度、光照和辐射由种植砖维持；" +
                "灌溉液和肥料仍需通过液体管道和复制人供应。\n" +
                "使用建筑详情面板下方的按钮，在完全隔绝、仅透气、透气透水之间循环切换。",
                "Current environment mode. It only controls exchange between the plant's cells and " +
                "the outside. The brick maintains the crop's pressure, temperature, light, and " +
                "radiation, but irrigation liquid and fertilizer still have to be supplied through " +
                "the liquid pipe and duplicant deliveries.\n" +
                "Use the button below the building details panel to cycle through Fully Sealed, " +
                "Gas Permeable, and Gas + Liquid Permeable.");

            UI.SPACEPLANTINGBRICK.TARGET_TEMP = L(zh,
                "目标温度：{0}（自动调节）",
                "Target temperature: {0} (auto)");
            UI.SPACEPLANTINGBRICK.TARGET_TEMP_TOOLTIP = L(zh,
                "太空种植砖会把植物的温度维持在这个数值。数值取自植物可正常生长的温度范围中点。",
                "The brick keeps the plant at this temperature. The value is the midpoint of the " +
                "plant's comfortable temperature range.");

            UI.SPACEPLANTINGBRICK.NUTRIENTS = L(zh,
                "所需资源：{0}",
                "Required resources: {0}");
            UI.SPACEPLANTINGBRICK.NUTRIENTS_TOOLTIP = L(zh,
                "这些是植物生长所需的灌溉物和肥料。液体灌溉需要通过液体管道输入砖体；" +
                "管道里不是该植物所需灌溉物的液体或气体不会被吸入；" +
                "气体灌溉作物可以接入气体管道，也可以由复制人手动搬运；固体肥料由复制人搬运。" +
                "种植砖不会自动补充资源。",
                "These are the irrigation and fertilizer the crop needs. Liquid irrigation must be " +
                "piped into the brick; liquids or gases in the pipe that are not the crop's " +
                "required irrigation are not absorbed; gas-irrigation crops can use a gas pipe or " +
                "manual duplicant delivery, and solid fertilizer is delivered by duplicants. The " +
                "brick does not supply resources for free.");

            UI.SPACEPLANTINGBRICK.GROW_LIGHT_ON = L(zh, "生长灯：开启", "Grow light: on");
            UI.SPACEPLANTINGBRICK.GROW_LIGHT_OFF = L(zh, "生长灯：关闭", "Grow light: off");
            UI.SPACEPLANTINGBRICK.GROW_LIGHT_TOOLTIP = L(zh,
                "检测到植物需要光照时，种植砖会点亮内置生长灯；喜欢黑暗的植物不会开灯。",
                "When the planted crop needs light, the brick turns on its integrated grow light. " +
                "Plants that prefer darkness keep it off.");
            UI.SPACEPLANTINGBRICK.NO_PLANT = L(zh, "未种植植物", "No plant");
        }

        private static bool UseChinese()
        {
            try
            {
                string code = Localization.GetCurrentLanguageCode();
                return !string.IsNullOrEmpty(code) && code.StartsWith("zh");
            }
            catch
            {
                return false;
            }
        }

        private static LocString L(bool chinese, string zh, string en)
        {
            return new LocString(chinese ? zh : en);
        }
    }
}
