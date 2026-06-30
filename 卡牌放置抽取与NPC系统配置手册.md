# 卡牌放置、抽取与NPC系统 配置手册

## 目录
- [1. 前言](#1-前言)
- [2. 前置依赖](#2-前置依赖)
- [3. 功能完成状态概览](#3-功能完成状态概览)
- [4. 卡牌数据SO配置](#4-卡牌数据so配置)
- [5. 宣言配置SO](#5-宣言配置so)
- [6. 预制体准备](#6-预制体准备)
  - [6.1 卡牌预制体 CardPrefab](#61-卡牌预制体-cardprefab)
  - [6.2 槽位预制体 SlotPrefab](#62-槽位预制体-slotprefab)
  - [6.3 宣言气泡预制体 DeclarationBubble](#63-宣言气泡预制体-declarationbubble)
- [7. 场景搭建](#7-场景搭建)
  - [7.1 整体层级结构](#71-整体层级结构)
  - [7.2 玩家手牌 PlayerHand](#72-玩家手牌-playerhand)
  - [7.3 NPC手牌 NPCHand](#73-npc手牌-npchand)
  - [7.4 战场 BattleField](#74-战场-battlefield)
  - [7.5 宣言气泡 DeclarationBubble](#75-宣言气泡-declarationbubble)
  - [7.6 游戏管理 GameDirector](#76-游戏管理-gamedirector)
- [8. 脚本字段引用对照表](#8-脚本字段引用对照表)
- [9. 已完成功能详解](#9-已完成功能详解)
- [10. 未完成功能规划](#10-未完成功能规划)
- [11. 运行测试](#11-运行测试)

---

## 1. 前言

本文档记录从「卡牌数据创建」到「完整可玩」的全流程配置步骤，包含：
- ScriptableObject 数据配置
- 预制体准备
- 场景层级搭建
- 脚本挂载与引用连线
- 测试方法

---

## 2. 前置依赖

确保项目中已安装以下包：
- **Unity Splines**（Package Manager → 搜索 Splines → Install）
- **DOTween**（导入资产后在 Tools → Demigiant → DOTween Utility Panel → Setup DOTween）

---

## 3. 功能完成状态概览

### ✅ 已完成功能

| 功能 | 状态 | 相关脚本 |
|------|------|----------|
| 弧形手牌布局 | ✅ 完成 | HandController.cs |
| 卡牌拖拽交互 | ✅ 完成 | CardDragHandler.cs |
| 槽位放置功能 | ✅ 完成 | SlotView.cs |
| 拖拽时高亮最近槽位 | ✅ 完成 | CardDragHandler.cs |
| 放置动画（DOTween） | ✅ 完成 | SlotView.cs |
| 手牌布局与拖拽冲突修复 | ✅ 完成 | HandController.cs |
| 卡槽与卡牌位置同步 | ✅ 完成 | SlotView.cs |
| 已放置卡牌不可选回手牌 | ✅ 完成 | CardDragHandler.cs |
| 玩家/NPC卡牌显示区分 | ✅ 完成 | CardView.cs |
| 宣言气泡显示 | ✅ 完成 | DeclarationView.cs |
| 宣言配置SO | ✅ 完成 | AIDeclarationConfig.cs |

### ❌ 未完成功能

| 功能 | 状态 | 预留接口 | 相关脚本 |
|------|------|----------|----------|
| 玩家回合流程控制 | ❌ 未完成 | - | - |
| 抽牌 → 放置/攻击模式切换 | ❌ 未完成 | - | - |
| 攻击系统状态机 | ❌ 未完成 | IBattleCalculator | AttackSystem.cs |
| 攻击判定接口实现 | ❌ 未完成 | IBattleCalculator | - |
| NPC自动布阵AI | ⚠️ 部分完成 | IAIStrategy | IAIStrategy.cs, RandomAIStrategy.cs |
| NPC自动出牌逻辑 | ❌ 未完成 | - | NPCPresenter.cs |

---

## 4. 卡牌数据SO配置

### 4.1 创建单张卡牌数据

1. 在 Project 窗口中，右键 → **Card → Card Data
2. 命名为 `CardData_xxx`（例如 `CardData_Spade_A`）
3. 在 Inspector 中填写：

| 字段 | 说明 | 示例 |
|------|------|------|
| Id | 卡牌唯一ID | 1 |
| Card Name | 卡牌名称 | "黑桃A" |
| Suit | 花色 | Spade |
| Rank | 点数 | Ace |
| Card Sprite | 牌面图片 | 拖拽对应Sprite |

### 4.2 批量创建

重复 4.1 步骤，为每种花色×点数创建所有需要的卡牌SO。

---

## 5. 宣言配置SO

### 5.1 设计理念

根据架构文档 §3.3，AI 宣言系统的核心设计：

> **不是随机说台词，而是基于 AI 布阵结果 + 难度等级，生成有策略意义的暗示信息。**

玩家看到 AI 宣言后，需要推理 AI 最强牌的位置，从而决定自己三张牌的出牌顺序。这是一个信息博弈系统。

### 5.2 宣言数据结构 AIDeclaration

```
AIDeclaration
├── PositionHint   // 位置提示文本（如"左边最强"、"中间这张牌是我的王牌"）
├── ValueHint      // 数值提示文本（如"我手里有一张A"、"别小看我的Q"）
└── InfoLevel     // 信息量等级（0~3）
```

**PositionHint** 告诉玩家哪张牌最强，**ValueHint** 告诉玩家最强牌是什么点数。难度越高，透露的信息越少。

### 5.3 信息量等级

| 难度 | InfoLevel | 透露信息 |
|------|-----------|----------|
| Easy（简单） | 3 | 位置提示 + 数值提示 |
| Normal（普通） | 2 | 随机透露位置提示或数值提示之一 |
| Hard（困难） | 1 | 模糊宣言（"这一局，我不会输"） |
| Nightmare（噩梦） | 0 | 最小提示（"……"） |

### 5.4 宣言生成流程

```
AI布阵（决定三张牌左→中→右的顺序）
        ↓
分析手牌：找出最强牌及其位置
        ↓
根据 InfoLevel 生成宣言
        ↓
DeclarationView 显示宣言气泡
        ↓
玩家根据宣言推理，选择攻击哪一侧
```

### 5.5 创建宣言配置资产

1. 在 Project 窗口中，右键 → **Game → AI Declaration Config**
2. 命名为 `AIDeclarationConfig`
3. 在 Inspector 中填写以下配置：

#### Position Templates（位置宣言模板）

| 占位符 | 含义 |
|--------|------|
| `{0}` | 最强牌位置（左边/中间/右边） |

示例配置：

| 模板文本 | 生成效果（假设最强在左边） |
|----------|--------------------------|
| `左中右，{0}最强` | 左中右，**左边**最强 |
| `{0}这张牌，是我的王牌` | **左边**这张牌，是我的王牌 |
| `看好我的{0}！` | 看好我的**中间**！ |
| `这局，{0}会给你惊喜` | 这局，**右边**会给你惊喜 |

#### Value Templates（数值宣言模板）

| 占位符 | 含义 |
|--------|------|
| `{0}` | 最强牌的完整名称（如"黑桃A"、"红桃Q"） |

示例配置：

| 模板文本 | 生成效果（假设最强是黑桃A） |
|----------|--------------------------|
| `我手里有一张{0}` | 我手里有一张**黑桃A** |
| `这张{0}，你可要小心了` | 这张**黑桃A**，你可要小心了 |
| `别小看我的{0}` | 别小看我的**方块Q** |
| `我最好的牌是{0}` | 我最好的牌是**黑桃A** |

#### Vague Templates（模糊宣言，Hard难度使用）

用于 InfoLevel ≤ 1 时：

```
……准备好了吗？
让我看看你的实力。
出招吧。
这一局，我不会输。
有点意思……
```

#### Minimal Templates（最小提示，Nightmare难度使用）

用于 InfoLevel = 0 时：

```
……
哼。
来吧。
别大意。
```

### 5.6 示例效果

假设 AI 手牌为：[红桃5, 黑桃A, 梅花Q]，最强牌是中间的黑桃A

| 难度 | InfoLevel | 生成效果 |
|------|-----------|----------|
| Easy | 3 | `左中右，**中间**最强，我手里有一张**黑桃A**` |
| Normal | 2 | `看好我的**中间**！` 或 `我最好的牌是**黑桃A**` |
| Hard | 1 | `让我看看你的实力。` |
| Nightmare | 0 | `……` |

---

## 6. 预制体准备

### 6.1 卡牌预制体 CardPrefab

路径：`Assets/Prefab/CardPrefab/CardPrefab.prefab`

**结构：
```
CardPrefab (RectTransform + CardView + CardDragHandler + CanvasGroup)
├── Image (Image + CanvasRenderer)    ← 牌面图片
├── CardBack (GameObject)            ← 卡背（可选）
├── SelectedHighlight (GameObject)    ← 选中高亮（可选）
└── Text (Legacy) (Text)             ← 牌名文本（可选）
```

**CardView 字段说明**：

| 字段 | 类型 | 说明 |
|------|------|------|
| Card Image | Image | 牌面图片组件（子物体Image，可留空，运行时自动查找） |
| Card Name Text | Text | 牌名文本（可留空，运行时自动查找） |
| Card Back | GameObject | 卡背（可选，没有就留空） |
| Selected Highlight | GameObject | 选中高亮框（可选） |

> CardView 会在 Awake 时自动在子物体中查找 Image 和 Text，所以**不需要手动拖拽引用**。

**CardDragHandler 字段说明**：

| 字段 | 类型 | 说明 |
|------|------|------|
| Snap Distance | float | 吸附判定距离（像素），默认 150 |
| Return Duration | float | 回弹动画时间，默认 0.3 |
| Snap Duration | float | 吸附动画时间，默认 0.25 |
| Drag Scale | float | 拖拽中缩放倍数，默认 1.2 |

### 6.2 槽位预制体 SlotPrefab

**创建步骤**：

1. Project 窗口右键 → UI → Image，命名为 `SlotPrefab`
2. 设置 Image 的 Source Image 为槽位底图（或用纯色半透明当占位）
3. **重要**：在 SlotPrefab 下创建一个子物体，命名为 `CardSlot`（用于放置卡牌的容器）
4. 添加组件 → 搜索 `SlotView`，添加
5. 拖入 `Assets/Prefab/` 目录做成预制体

**SlotView 字段说明**：

| 字段 | 类型 | 说明 |
|------|------|------|
| Slot Index | int | 槽位索引（运行时由 BattleFieldView 自动设置） |
| Is Player Slot | bool | 是否是玩家槽位 |
| Slot Background | Image | 槽位背景图（可留空，自动获取自身Image） |
| Highlight Frame | GameObject | 高亮边框（可选） |
| Normal Color | Color | 正常状态颜色 |
| Highlighted Color | Color | 高亮状态颜色（拖拽时高亮） |
| Occupied Color | Color | 占用状态颜色 |
| Attack Target Color | Color | 攻击目标高亮颜色（红色） |
| Place Duration | float | 放置动画时长 |
| Place Ease | Ease | 放置动画缓动 |
| Interactable | bool | 是否可交互 |

### 6.3 宣言气泡预制体 DeclarationBubble

**创建步骤**：

1. Project 窗口右键 → UI → Image，命名为 `DeclarationBubble`
2. 设置 Source Image 为气泡背景图（或纯色圆角矩形）
3. 在其子下创建一个 UI → Text，命名为 `DeclarationText`
4. 给根物体添加组件 → 搜索 `DeclarationView`，添加
5. 拖入 `Assets/Prefab/` 目录做成预制体

**DeclarationView 字段说明**：

| 字段 | 类型 | 说明 |
|------|------|------|
| Bubble Root | GameObject | 气泡根物体（可留空，用自身） |
| Declaration Text | Text | 宣言文本组件 |
| Npc Portrait | Image | NPC头像（可选） |
| Show Duration | float | 弹出动画时长 |
| Hide Duration | float | 收起动画时长 |
| Default Display Time | float | 默认显示时长 |
| Show Ease / Hide Ease | Ease | 弹入/弹出缓动 |
| Use Typewriter | bool | 是否启用打字机效果 |
| Typewriter Speed | float | 打字机速度 |

---

## 7. 场景搭建

### 7.1 整体层级结构

```
Canvas
├── BattleField          (空物体，挂 BattleFieldView)
│   ├── PlayerSlots      (空物体，Layout Group 或手动排列)
│   │   ├── Slot_0       (SlotPrefab 实例)
│   │   │   └── CardSlot ← 重要：卡牌容器子物体
│   │   ├── Slot_1
│   │   └── Slot_2
│   └── NPCSlots         (空物体)
│       ├── Slot_0       (SlotPrefab 实例，isPlayerSlot=false)
│       ├── Slot_1
│       └── Slot_2
├── PlayerHand           (空物体，挂 HandController，玩家手牌)
├── NPCHand              (空物体，挂 HandController，NPC手牌)
├── DeclarationBubble     (DeclarationBubble 预制体实例)
└── GameDirector         (空物体，挂 HandPresenter + NPCPresenter)
```

### 7.2 玩家手牌 PlayerHand

1. 在 Canvas 下创建空物体，命名 `PlayerHand`
2. 添加组件 `HandController`
3. 位置：摆放在屏幕下方
4. **关键参数设置**：

| 参数 | 值 | 说明 |
|------|-----|------|
| Spline Container | 留空 | 运行时自动创建弧形样条 |
| Auto Arc Width | 800 | 弧形宽度 |
| Auto Arc Height | 100 | 弧形弯曲深度 |
| Arc Downward | ☑ 勾选 | 弧形向下弯（玩家手牌朝下弯） |
| Start Normalized Pos | 0.05 | 卡牌起始位置 |
| End Normalized Pos | 0.95 | 卡牌结束位置 |
| Orient To Tangent | ☑ 勾选 | 卡牌朝向曲线切线 |
| Hover Y Offset | 60 | 悬停上移距离 |
| Hover Scale | 1.15 | 悬停放大倍数 |
| Debug Card Prefab | CardPrefab | 调试用预制体（可选） |

### 7.3 NPC手牌 NPCHand

1. 在 Canvas 下创建空物体，命名 `NPCHand`
2. 添加组件 `HandController`
3. 位置：摆放在屏幕上方
4. **关键参数设置**（与玩家手牌基本一致，除了以下区别）：

| 参数 | 值 | 说明 |
|------|-----|------|
| Arc Downward | ☐ 不勾选 | 弧形向上弯（NPC手牌朝上弯） |
| Debug Card Prefab | CardPrefab | 同左 |

> NPC手牌默认显示牌背朝上（因为 `CardView.Hide()` 会隐藏正面）

### 7.4 战场 BattleField

1. 在 Canvas 下创建空物体，命名 `BattleField`
2. 添加组件 `BattleFieldView`
3. 在其下创建 `PlayerSlots` 空物体
4. 从 Project 窗口把 `SlotPrefab` 拖 3 个到 PlayerSlots 下，命名为 `Slot_0`、`Slot_1`、`Slot_2`
5. 调整 3 个槽位水平排列（可用 Horizontal Layout Group 或手动摆位置）
6. 同样创建 `NPCSlots`，拖 3 个 SlotPrefab 实例

**BattleFieldView 字段拖拽**：

| 字段 | 拖拽对象 |
|------|----------|
| Player Slots | 把 PlayerSlots 下的 3 个 Slot_0/1/2 拖入数组 |
| NPC Slots | 把 NPCSlots 下的 3 个 Slot_0/1/2拖入数组 |
| Is Player Turn | ☑ 勾选（默认玩家回合） |

### 7.5 宣言气泡 DeclarationBubble

1. 从 Project 把 `DeclarationBubble` 预制体拖到 Canvas 下
2. 位置：放在 NPC 头像旁边（右上角或中上位置）
3. DeclarationView 的字段无需额外配置（自动找自身 Image 和 Text）

### 7.6 游戏管理 GameDirector

1. 在 Canvas 下创建空物体，命名 `GameDirector`
2. 添加组件 `HandPresenter`
3. 添加组件 `NPCPresenter`

---

## 8. 脚本字段引用对照表

### 8.1 HandController (玩家手牌)

挂在：`Canvas/PlayerHand`

| 字段 | 值 |
|------|-----|
| Spline Container | 留空（自动生成） |
| Auto Arc Width | 800 |
| Auto Arc Height | 100 |
| Arc Downward | ☑ |
| Debug Card Prefab | CardPrefab |

### 8.2 HandController (NPC手牌)

挂在：`Canvas/NPCHand`

| 字段 | 值 |
|------|-----|
| Spline Container | 留空 |
| Auto Arc Width | 800 |
| Auto Arc Height | 100 |
| Arc Downward | ☐ |
| Debug Card Prefab | CardPrefab |

### 8.3 BattleFieldView

挂在：`Canvas/BattleField`

| 字段 | 值 |
|------|-----|
| Player Slots | 3个玩家槽位(SLot_0/1/2) |
| NPC Slots | 3个NPC槽位(SLot_0/1/2) |
| Is Player Turn | ☑ |

### 8.4 DeclarationView

挂在：`Canvas/DeclarationBubble`

| 字段 | 值 |
|------|-----|
| Bubble Root | 留空（自身） |
| Declaration Text | 子物体 Text |
| Default Display Time | 2.5 |

### 8.5 HandPresenter

挂在：`Canvas/GameDirector`

| 字段 | 拖拽对象 |
|------|----------|
| Hand Controller | Canvas/PlayerHand |
| Card Prefab | CardPrefab.prefab |
| Battle Field | Canvas/BattleField |
| Draw Pile Transform | 牌堆位置的RectTransform（可选，没有则无抽牌动画） |
| Draw Card Duration | 0.4 |
| Draw Interval | 0.1 |

### 8.6 NPCPresenter

挂在：`Canvas/GameDirector`

| 字段 | 拖拽对象 |
|------|----------|
| Npc Hand Controller | Canvas/NPCHand |
| Battle Field | Canvas/BattleField |
| Declaration View | Canvas/DeclarationBubble |
| Card Prefab | CardPrefab.prefab |
| Declaration Config | AIDeclarationConfig.asset |
| Difficulty | AI难度（Easy/Normal/Hard/Nightmare） |
| Debug Show Front | ☐（调试时可勾选查看NPC手牌） |

---

## 9. 已完成功能详解

### 9.1 弧形手牌布局 (HandController)

使用 Unity Splines 包实现卡牌沿弧形曲线均匀分布，配合 DOTween 实现平滑的布局动画。

**核心特性**：
- 自动创建弧形样条曲线
- 支持悬停时卡牌上移放大
- 布局动画支持延迟播放（每张卡牌依次动画）
- 拖拽时自动暂停布局刷新（避免位置冲突）

**关键方法**：
```csharp
AddCard(GameObject card, bool refreshLayout = true)   // 添加卡牌
RemoveCard(GameObject card, bool refreshLayout = true) // 移除卡牌
PauseLayout()   // 暂停布局刷新（拖拽时调用）
ResumeLayout(bool refreshImmediate = true)  // 恢复布局刷新
UpdateCardLayout()  // 手动刷新布局
```

### 9.2 卡牌拖拽交互 (CardDragHandler)

实现卡牌从手牌到槽位的拖拽放置，支持瞬移跟手模式和槽位吸附高亮。

**交互流程**：
1. **拖拽开始**：缓存 HandController 引用，暂停手牌布局，清除槽位高亮
2. **拖拽中**：卡牌瞬移跟随鼠标，实时检测并高亮最近的空槽位
3. **拖拽结束**：
   - 找到最近的空槽位 → 吸附放置
   - 未找到 → 回弹到手牌位置

**关键特性**：
- 瞬移模式：无延迟跟手，直接设置 transform.position
- 自动高亮：拖拽时实时高亮最近的空槽位
- 偏移保持：拖拽开始时记录鼠标与卡牌的偏移，放置时保持相对位置

### 9.3 槽位放置功能 (SlotView)

卡牌放置到槽位时，自动调整锚点、位置、旋转和缩放，使卡牌与槽位完全贴合。

**放置逻辑**：
1. 记录卡牌当前世界位置
2. 将卡牌父物体设为槽位的 CardSlot 子物体
3. 设置卡牌锚点为 (0.5, 0.5)，anchoredPosition 为 (0, 0)
4. 计算等比缩放使卡牌填满槽位
5. 播放放置动画（位置、旋转、缩放同时过渡）

**关键特性**：
- 使用 CardSlot 子物体作为容器（避免 Layout Group 影响锚点）
- 只调整缩放适配槽位大小，位置和旋转归零
- 放置后卡牌状态变为 CardState.Placed

### 9.4 手牌布局与拖拽冲突修复

解决了以下问题：
- 卡牌放置后位置错乱（因为仍在 HandController.cards 列表中）
- cards.Count 永远是 5（已放置的卡牌未移除）
- ResumeLayout 有时不生效（找不到 HandController）

**解决方案**：
1. `CleanupInvalidCards()`：每次布局刷新前清理父物体不是自己的卡牌
2. 缓存 HandController 引用：避免放置后找不到
3. 防御性检查：`AddCard`/`RemoveCard` 增加防重复检查

---

## 10. 未完成功能规划

### 10.1 玩家回合流程控制

**目标**：实现完整的玩家回合流程：
1. 玩家回合开始
2. 抽一张牌
3. 判断是否有空槽位
   - 有空槽位 → 进入放置模式（提示"请放置卡牌"）
   - 无空槽位 → 进入攻击模式（提示"请选择攻击"）
4. 回合结束

**待实现**：
- GameTurnManager：管理回合流程状态机
- UI 提示系统：显示当前操作提示
- 模式切换逻辑：在放置/攻击模式之间切换

### 10.2 攻击系统

**目标**：实现卡牌攻击功能：
1. 玩家选择自己的一张已放置卡牌作为主攻牌
2. 玩家选择敌方的一张卡牌作为目标
3. 执行攻击判定
4. 根据判定结果更新卡牌状态

**已完成**：
- `AttackSystem.cs`：攻击状态机框架
- `IBattleCalculator` 接口：预留攻击判定逻辑

**待实现**：
- 攻击判定逻辑（IBattleCalculator 实现）
- 攻击动画
- 攻击结果应用（胜负判定）

### 10.3 NPC自动出牌

**目标**：NPC 能够自动布阵和出牌

**已完成**：
- `IAIStrategy` 接口：定义 AI 策略接口
- `RandomAIStrategy`：随机布阵策略

**待实现**：
- 更多 AI 策略实现（Easy/Normal/Hard/Nightmare）
- NPC 出牌时机控制
- NPC 回合流程整合

---

## 11. 运行测试

### 11.1 宣言系统验证（优先测试）

宣言系统的核心是**信息博弈**，按以下流程验证：

```
1. AI 初始化手牌（3张）
         ↓
2. AI 布阵（根据难度策略决定左→中→右顺序）
         ↓
3. AI 分析手牌找出最强牌
         ↓
4. 根据难度生成宣言（位置提示 +/或 数值提示）
         ↓
5. DeclarationView 显示宣言气泡
         ↓
6. 玩家根据宣言决定攻击哪一侧
```

**测试要点**：
- 切换不同难度，观察宣言透露的信息量是否正确
- Easy：应同时显示位置+数值
- Normal：应只显示位置或数值之一
- Hard：应显示模糊宣言
- Nightmare：应只显示"……"

### 11.2 拖拽放置测试

**测试要点**：
1. 运行场景，观察玩家手牌是否以弧形展开在下方
2. 鼠标悬停玩家卡牌，是否有上移 + 放大效果
3. **拖拽卡牌**，观察是否有槽位高亮提示最近空槽位
4. 拖拽到槽位附近（150像素内），是否自动吸附放置
5. 拖拽到其他位置，是否回弹到手牌中
6. 放置后卡牌是否与槽位位置/旋转/大小一致
7. 放置后剩余手牌是否正确重新排列

### 11.3 完整流程测试步骤

1. 运行场景
2. 观察玩家手牌是否以弧形展开在下方（HandController 弧形排列）
3. 观察 NPC 手牌是否以弧形展开在上方（弧形朝上，牌背朝上）
4. 鼠标悬停玩家卡牌，是否有上移 + 放大效果
5. 拖拽一张玩家卡牌到槽位，是否放置成功
6. **观察宣言气泡**：根据当前 AI 难度，验证显示内容是否正确
7. **切换难度测试**：在 Inspector 中修改 `Difficulty`，重新运行验证信息量变化

### 11.4 常见问题

| 问题 | 可能原因 | 解决方法 |
|------|----------|----------|
| 卡牌不显示图片 | CardPrefab的Image子物体没图 | 检查CardPrefab的Image是否有Source Image |
| 卡牌没有弧形排列 | HandController的Spline没生成 | 检查Splines包是否安装 |
| DOTween动画无效 | DOTween没Setup | Tools→Demigiant→DOTween Utility Panel→Setup |
| 拖拽时卡牌乱飞 | HandController.UpdateCardLayout冲突 | 确保拖拽时调用了PauseLayout() |
| 放置后卡牌位置错乱 | 卡牌仍在HandController.cards列表 | 确保调用了RemoveCard()或CleanupInvalidCards()生效 |
| 槽位高亮不显示 | CardDragHandler未正确检测槽位 | 检查snapDistance是否足够大 |
| 卡牌放置不贴合槽位 | SlotPrefab缺少CardSlot子物体 | 确保创建了CardSlot子物体作为卡牌容器 |

---

## 更新日志

| 日期 | 更新内容 |
|------|----------|
| 2026-06-29 | 初始版本，区分已完成/未完成功能 |
