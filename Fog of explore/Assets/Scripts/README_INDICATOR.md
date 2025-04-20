# 随机化Sprite指示器使用指南

## 概述

本系统允许您将随机化的Sprite对象与屏幕外指示器系统集成，使玩家能够看到视野外随机生成的对象的方向指示器。指示器可以使用与游戏对象相同的Sprite作为图标，并附加箭头指示方向。

## 组件说明

系统包含以下几个主要组件：

1. **IndicatorManager** - 管理所有屏幕外指示器的显示逻辑
2. **RandomizedSpriteIndicator** - 将随机化的Sprite与指示器系统连接的组件
3. **SpriteRandomizer** - Unity编辑器工具，用于随机化Sprite并可选择性地添加指示器
4. **IndicatorPrefabCreator** - 编辑器工具，用于创建自定义指示器预制体

## 使用方法

### 基本设置

1. 确保场景中有一个带有 `IndicatorManager` 组件的游戏对象
2. 设置 `IndicatorManager` 的 `player` 引用为您的玩家角色
3. 可选：设置 `defaultIndicatorPrefab` 为您希望使用的默认指示器预制体

### 创建指示器预制体

1. 在Unity编辑器中，选择菜单：`Tools -> Indicator Prefab Creator`
2. 设置预制体名称和图标Sprite（可以暂时为空，稍后会被替换为对象的Sprite）
3. 设置图标大小和颜色
4. 配置箭头样式、大小和颜色
5. 点击"创建预制体"按钮生成预制体

### 使用SpriteRandomizer随机化并添加指示器

1. 在Unity编辑器中，选择菜单：`Tools -> Sprite Randomizer`
2. 选择包含要随机化的SpriteRenderer组件的父游戏对象
3. 添加您想要随机使用的Sprite
4. **为每个Sprite单独设置是否显示指示器**（勾选每个Sprite旁边的"指示器"选项）
5. 在"Indicator Settings"部分，勾选"启用指示器功能"选项
6. 选择一个指示器预制体（必须是 `IndicatorOffScreen` 类型）
7. 设置显示条件（最小距离、最大距离等）
8. 点击"Randomize Sprites"按钮应用随机化和指示器设置

### 手动添加指示器

如果您想手动为特定对象添加指示器：

1. 选择目标游戏对象（必须包含 `SpriteRenderer` 组件）
2. 添加 `RandomizedSpriteIndicator` 组件
3. 设置 `indicatorPrefab` 为您想使用的指示器预制体
4. 设置 `useCustomSprite` 为 true，并指定 `customSprite`（或保持为 false 使用当前的精灵）
5. 如果需要，调整距离设置和其他选项

## 示例设置

### 设置IndicatorManager

```csharp
// 将此组件添加到场景中的一个游戏对象
// 设置player引用为玩家角色
// 如果需要，设置displayDistance和defaultIndicatorPrefab
```

### 手动设置RandomizedSpriteIndicator

```csharp
// 添加到带有SpriteRenderer的游戏对象
// 设置indicatorPrefab
// 设置useCustomSprite和customSprite（如需自定义图像）
// 如果需要，调整minDistance和maxDistance
```

## 指示器与Sprite匹配

系统支持两种使用Sprite作为指示器图像的方式：

1. **自动匹配** - 当 `useCustomSprite` 为 false 时，指示器会自动使用对象当前的 SpriteRenderer.sprite
2. **自定义Sprite** - 当 `useCustomSprite` 为 true 时，指示器会使用 `customSprite` 属性指定的Sprite

通过SpriteRandomizer工具，您可以：
- 为特定类型的Sprite自动添加指示器（勾选对应Sprite的"指示器"选项）
- 所有带有指示器的对象会使用其实际显示的Sprite作为指示器图像

## 注意事项

- 指示器只有在物体与玩家之间的距离满足条件时才会显示
- 物体必须有SpriteRenderer组件才能使用RandomizedSpriteIndicator
- 指示器预制体必须是IndicatorOffScreen类型
- 指示器图像组件需要是Image类型，并且位于预制体层次结构中

## 疑难解答

如果指示器不显示，请检查：

1. IndicatorManager是否正确设置，并且player引用是否有效
2. 随机化对象是否有效，SpriteRenderer是否可见
3. 指示器预制体是否正确设置，是否包含Image组件
4. 距离条件是否满足显示要求

如果指示器不使用对象的Sprite，请检查：
1. Image组件是否存在于指示器预制体中
2. useCustomSprite是否设置正确 