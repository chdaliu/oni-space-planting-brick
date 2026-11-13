[中文](#chinese) | [English](#english)

<a id="chinese"></a>

# 太空种植砖（ONI-SpacePlantingBrick）

《缺氧》Mod：添加一块 1x1 的 **太空种植砖**，把一小片可居住的土壤带进真空、极寒或极热的地方。

## 特性

- **自动温度调节**：读取植物可正常生长的温度范围，并把植物维持在该范围的中点。
- **需要实际资源**：不再免费提供灌溉液和肥料。液体灌溉需要通过液体管道输入砖体；以气体为灌溉物的作物可以接入气体管道，也可以由复制人手动搬运；固体肥料由复制人搬运。作物仍会正常消耗资源。
- **自带生长灯**：需要光照的植物会自动点亮砖内生长灯；喜欢黑暗的植物不会开灯。
- **与原版液培砖同尺寸同位置**：世界贴图使用原版液培砖的 248 x 248 边界、124 x 124 像素贴图，以及原版动画里的帧偏移；建造图标使用原版 244 x 228 边界和 122 x 114 像素图标。
- **液培砖改色**：直接沿用原版液培砖的贴图，保留金属框架、营养液槽、生长介质、液位观察窗和进液口，只改颜色：钢材偏蓝灰、营养液青色、基质深靛蓝；形状、尺寸、位置和放置蓝图都不变。
- **可倒置种植**：和原版液培砖一样使用 `Rotatable` + `FlipV` 的旋转逻辑。正立时植物向上生长，倒置时植物向下生长，适合火椒等垂吊植物；建造时用旋转键（默认 `O`）翻转即可，不再额外添加“种植方向”按钮。
- **环境切换按钮**（位于建筑详情面板下方的功能区，三档循环）
  - **完全隔绝**（默认）：植物所在格不与外界交换热量、气体和液体。
  - **仅透气**：气体和热量可以进出植物所在格，但液体不能进入。
  - **透气透水**：气体、液体和热量都可以正常进出植物所在格。
- **环境需求全包**：无论选择哪种环境模式，植物所需的气压、温度、光照和辐射条件都由种植砖维持；外界气压、气体种类、光照、温度、辐射和液体不会让植物枯萎或淹死。灌溉液/气体和肥料仍需自行供应。
- **与液培砖同科技**：和原版液培砖一起在“农业”科技解锁，建造菜单位于“食物 → 农业”。
- **材料**：100 kg 泥土 + 50 kg 金属矿石或精炼金属。
- **中英文界面**：根据游戏语言自动显示中文或英文。

## 使用

1. 在“食物 → 农业”中建造太空种植砖。
2. 像使用普通种植砖一样选择并种下种子。
3. 把液体管道接到砖体以供应灌溉液；气草等以气体为灌溉物的作物可接入气体管道。固体肥料由复制人搬运；需要光照的植物会自动获得光照。
4. 用详情面板下方功能区的“环境”按钮循环切换完全隔绝 / 仅透气 / 透气透水。
5. 需要倒立种植时（例如火椒），在建造时使用旋转键（默认 `O`）翻转砖体。

## 说明

- 完全隔绝模式会封锁植物所占据的非固体格子，并隔绝砖体所在格的热传导；复制人仍然可以正常经过和照料植物。
- 无论选择哪种环境模式，植物的气压、温度、光照和辐射条件都由种植砖维持；外界气压、气体种类、光照、温度、辐射和液体不会让植物枯萎或淹死。灌溉液/气体和肥料仍需自行供应。
- 仅透气模式只影响格子的气体/液体交换：恢复气体和热量交换，但保留液体隔绝。
- 透气透水模式恢复这些格子的正常气体、液体和热量交换；环境需求仍由种植砖维持。
- 倒置建造时砖体仍在原来的格子里，植物种在下方；完全隔绝模式会同时封锁砖体和植物所在的格子。
- 液体进液口是实际的液体管道接口，需要连接液体管道来供应灌溉液。
- 以气体为灌溉物的作物（例如气草需要氯气）可以接入砖体的气体输入口，也可以由复制人手动搬运；不接气体管道时会自动保留手动搬运方式。
- 固体肥料仍需复制人搬运。
- 砖内存储有物品时，可以使用建筑菜单中的“清空存储”按钮，把送错的液体、气体或肥料倒到附近地面。
- 作物会正常消耗管道输入的液体/气体和搬运来的肥料，不再有免费资源。

## 构建

需要 .NET SDK（`dotnet`）和 Python 3 + Pillow（仅用于重新生成动画）。

```bash
./build.sh
```

构建完成后会自动部署到：

```text
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-SpacePlantingBrick
```

## 文件结构

```text
src/
  SpacePlantingBrickConfig.cs    建筑定义、材料、科技与建造菜单
  SpacePlantingGasInput.cs       气体灌溉输入与管道端点
  SpacePlantingBrick.cs          温度、资源供应方式、光照、格子隔绝逻辑
  SpacePlantingBrickButtons.cs   环境切换按钮
  SpacePlantingBrickPatches.cs   植物环境免疫补丁（气压/光照/温度/淹水/辐射）
  SpacePlantingBrickMod.cs       Mod 入口、本地化与科技注册
  STRINGS.cs                     中英文字符串
anim/assets/space_planting_brick/
  *_0.png / *_build.bytes / *_anim.bytes
tools/generate_anim.py           从原版液培砖贴图改色并生成 kanim
tools/vanilla/                   原版液培砖图集（改色输入）
```

<a id="english"></a>

# Space Planting Brick (ONI-SpacePlantingBrick)

An **Oxygen Not Included** mod that adds a 1x1 **Space Planting Brick**, bringing a small patch of livable soil to vacuum, extreme cold, or extreme heat.

## Features

- **Automatic temperature control**: reads the crop's comfortable temperature range and keeps the plant at its midpoint.
- **Requires real resources**: irrigation liquid and fertilizer are no longer supplied for free. Liquid irrigation must be piped into the brick; crops that use a gas as irrigation can use the optional gas pipe or manual duplicant delivery, and solid fertilizer is delivered by duplicants. The crop consumes the resources normally.
- **Integrated grow light**: light-loving crops automatically receive the brick's grow light; crops that prefer darkness keep it off.
- **Vanilla Hydroponic Farm size and placement**: the world symbol uses the vanilla 248 x 248 bounds, 124 x 124 px sprite, and the vanilla animation-frame offset. The build icon uses the vanilla 244 x 228 bounds and 122 x 114 px icon.
- **Hydroponic-farm recolour**: reuses the vanilla Hydroponic Farm art, keeping its metal frame, nutrient reservoir, grow bed, sight glass, and liquid port. Only the palette changes: blue-grey steel, cyan nutrient solution, and deep-indigo growing medium. The shape, size, placement, and placement blueprint stay identical to the vanilla tile.
- **Invertible planting**: uses the same `Rotatable` + `FlipV` rotation logic as the vanilla Hydroponic Farm. Upright crops grow above it; inverted crops grow below it, which suits hanging plants such as Pincha Pepper. Flip it with the rotate key (`O` by default) while placing; no extra **Planting direction** button is added.
- **Environment button** (in the action area below the building details panel, three levels)
  - **Fully Sealed** (default): the plant's cells exchange no heat, gas, or liquid with the outside.
  - **Gas Permeable**: gas and heat pass through the plant's cells, but liquid cannot enter.
  - **Gas + Liquid Permeable**: gas, liquid, and heat flow through the plant's cells normally.
- **All environment needs supplied**: in every environment mode, the brick maintains the crop's pressure, temperature, light, and radiation conditions. External pressure, gas type, light, temperature, radiation, or liquid will not wilt or drown the crop. Irrigation liquid/gas and fertilizer still have to be supplied.
- **Same tech as the Hydroponic Farm**: unlocked in Agriculture, listed under Food → Farming.
- **Materials**: 100 kg dirt + 50 kg metal ore or refined metal.
- **Chinese and English UI**: text follows the current game language.

## Usage

1. Build the Space Planting Brick under Food → Farming.
2. Select and plant a seed exactly like a normal farm tile.
3. Connect a liquid pipe to supply irrigation; gas-irrigation crops such as Gas Grass can use the gas pipe. Let duplicants deliver solid fertilizer; light-loving crops receive light automatically.
4. Use the **Environment** button in the action area below the details panel to cycle through Fully Sealed, Gas Permeable, and Gas + Liquid Permeable.
5. To grow a hanging crop such as Pincha Pepper, use the rotate key (`O` by default) while placing the brick to flip it.

## Notes

- Fully Sealed seals every non-solid cell occupied by the crop and insulates the brick cell. Duplicants can still walk through and tend the plant.
- In every environment mode, the brick maintains the crop's pressure, temperature, light, and radiation conditions; external pressure, gas type, light, temperature, radiation, or liquid will not wilt or drown it. Irrigation liquid/gas and fertilizer still have to be supplied.
- Gas Permeable only changes cell exchange: gas and heat pass, liquid stays blocked.
- Gas + Liquid Permeable restores normal gas, liquid, and heat exchange; the brick still maintains all environment needs.
- When built inverted, the brick stays in its cell and the crop is planted below it. Fully Sealed seals both the brick cell and the crop cells.
- The liquid port is a working liquid-pipe connection; connect a liquid pipe to supply irrigation.
- Crops with gas irrigation (for example Gas Grass with Chlorine) can use the brick's gas input, or fall back to manual duplicant delivery when no gas pipe is connected.
- Solid fertilizer still has to be delivered by duplicants.
- When the internal storage is not empty, the building menu's **Empty Storage** button lets duplicants dump wrong liquids, gases, or fertilizer onto the ground.
- Crops consume piped liquid/gas and delivered fertilizer normally; there are no free resources.

## Build

Requires the .NET SDK (`dotnet`) and Python 3 + Pillow (only for regenerating the animation).

```bash
./build.sh
```

The build is deployed to:

```text
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-SpacePlantingBrick
```

## Project layout

```text
src/
  SpacePlantingBrickConfig.cs    Building definition, materials, tech, build menu
  SpacePlantingGasInput.cs       Gas irrigation input and conduit endpoint
  SpacePlantingBrick.cs          Temperature, resource supply, light, cell sealing
  SpacePlantingBrickButtons.cs   Environment toggle button
  SpacePlantingBrickPatches.cs   Crop environment immunity patches (pressure, light, temperature, drowning, radiation)
  SpacePlantingBrickMod.cs       Mod entry, localization, tech registration
  STRINGS.cs                     Chinese/English strings
anim/assets/space_planting_brick/
  *_0.png / *_build.bytes / *_anim.bytes
tools/generate_anim.py           Recolours the vanilla Hydroponic Farm art and writes the kanim
tools/vanilla/                   Vanilla Hydroponic Farm atlas used as the recolour input
```
