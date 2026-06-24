# Card

项目代号：金海豚。

这是一个基于 Unity 2022.3 的扑克牌原型工程。当前版本已经完成了卡牌数据建模、牌堆创建与洗牌、玩家抽 3 张牌，以及通过预制体把手牌显示到场景中的基础流程。

## 当前实现

- `CardData.cs`：定义单张牌的运行时数据结构，包含 `id`、`suit`、`rank`、`sprite`，并提供牌名拼接。
- `CardDataSO.cs`：定义卡牌 `ScriptableObject` 数据，可在 Unity 编辑器中配置花色、点数和牌面图片。
- `CardEnums.cs`：集中定义花色、点数、卡牌类型、战斗结果和花色效果枚举。
- `CardManager.cs`：负责组建牌堆、洗牌、抽牌，并把玩家手牌实例化到 UI 卡槽。
- `CardView.cs`：负责把 `CardDataSO` 中的牌面图片显示到卡牌预制体上。

## 当前流程

场景启动后，`CardManager` 会执行以下步骤：

1. 从 `allCards` 列表生成牌堆。
2. 对牌堆进行随机洗牌。
3. 为玩家抽取 3 张手牌。
4. 将 `cardPrefab` 实例化到 `playerCardSlots` 中。
5. 通过 `CardView.Init()` 设置每张牌的显示图片。

## 目录结构

```text
Assets/
|-- Prefab/
|   `-- CardPrefab/
|       `-- CardPrefab.prefab
|-- Scenes/
|   `-- SampleScene.unity
`-- Scripts/
    |-- Data/
    |   |-- CardData.cs
    |   |-- CardDataSO.cs
    |   `-- CardEnums.cs
    |-- Manager/
    |   `-- CardManager.cs
    |-- SOData/
    |   `-- CardData.asset
    `-- View/
        `-- Prefab/
            `-- CardView.cs
```

## 运行环境

- Unity `2022.3.62f3`
- 主要依赖：`com.unity.ugui`、`com.unity.textmeshpro`、`com.unity.feature.2d`

## 使用方式

1. 用 Unity Hub 打开本项目。
2. 打开场景 `Assets/Scenes/SampleScene.unity`。
3. 在场景中的 `CardManager` 上完成以下绑定：
   - `All Cards`：拖入卡牌 `CardDataSO` 资源。
   - `Card Prefab`：绑定 `Assets/Prefab/CardPrefab/CardPrefab.prefab`。
   - `Player Card Slots`：指定 3 个 UI 卡槽 `Transform`。
4. 确保 `CardPrefab` 上挂有 `CardView`，并正确绑定 `cardImage`。
5. 点击 Play，即可看到洗牌后抽出的 3 张手牌。

## 当前注意事项

- 当前仓库内只有一个示例 `CardData.asset`，而 `CardManager` 的完整流程预期是配置 52 张卡牌数据。
- `CardView` 目前只负责显示牌面图片，还没有加入点击、翻牌或出牌交互。
- 牌型结算和花色效果枚举已经预留，但还没有在管理器中接入战斗逻辑。

## 后续建议

- 补齐 52 张 `CardDataSO` 资源和对应牌面图片。
- 增加发牌动画、出牌逻辑和回合控制。
- 把花色效果与战斗结算接入 `CardManager` 或独立的战斗系统。
- 为抽牌、洗牌和结算流程补充 Play Mode / Edit Mode 测试。
