# Level Design Rules for KatanaRush

Rules for creating level segment prefabs that work with the procedural level generation system.

---

## 1. Prefab Hierarchy

```
[LevelName] (root)
├── Spawnpoints
│   ├── [EnemySpawnPoint objects]
│   └── [ObstacleSpawnPoint objects]
├── Collectibles
│   └── [CollectibleSpawnPoint objects]
└── Environment                      (visual root for SegmentReveal)
    ├── Level                        [container for cubes and slopes]
    │   ├── cube (1)
    │   ├── cube (2)
    │   ├── slope (1)
    │   └── ...
    ├── Plane                        [MeshFilter + MeshRenderer + MeshCollider, tag=Ground]
    └── Background
```

### Root GameObject

- **Components**: `LevelSegment`, `SegmentReveal`
- **Transform**: `localPosition = (0, 0, -10)`
- **Tag**: Untagged, **Layer**: Default

### Children

| Child | Contents |
|-------|---------|
| `Spawnpoints` | Enemy and obstacle spawn points only |
| `Collectibles` | All collectible spawn points (coins, magnets, diamonds, etc.) |
| `Environment` | Visual root referenced by SegmentReveal. Contains Level, Plane, and Background |
| `Environment/Level` | All geometry primitives: cubes and slopes only |
| `Environment/Plane` | Ground plane (biomes that use one) |
| `Environment/Background` | Decorative background objects |

> **Note**: Some older levels have collectibles misplaced under Spawnpoints. This is a copy-paste mistake from early development. New levels MUST place all `CollectibleSpawnPoint` objects under `Collectibles`.

---

## 2. Transform Constants

| Transform | localPosition |
|-----------|--------------|
| Root | `(0, 0, -10)` |
| Spawnpoints | `(0, 0, 0)` |
| Collectibles | `(0, 0, 0)` |
| Environment | `(0, 0, 27)` |
| Environment/Level | `(0, 0, 0)` relative to Environment |
| Background | `(0, 0, -17)` relative to Environment |

These values apply to all biomes. Desert older levels may show different values — ignore them, use the values above.

---

## 3. LevelSegment Settings

| Property | Constraints |
|----------|-------------|
| `segmentLength` | Always **100** |
| `difficultyWeight` | `0` = easy, `1` = hard |
| `allowConsecutive` | Whether this segment can appear back-to-back |

### Difficulty Rules

- **0**: Standard levels with Static enemies
- **1**: Hard levels with RocketLauncher enemies

---

## 4. SegmentReveal Settings

Fixed values, do not change:

| Property | Value |
|----------|-------|
| `revealDuration` | 0.6 |
| `dropDistance` | 8 |
| `revealEase` | 27 (OutBack) |
| `overshoot` | 1.2 |
| `visualRoot` | → Environment child transform |
| `staggerChildren` | false |

---

## 5. Ground Plane

Present as a direct child of `Environment` (NOT inside `Level`). Tag = `Ground`, Layer = 3.
Components: `MeshFilter` (Plane mesh) + `MeshRenderer` + `MeshCollider`.
Cubes and slopes go inside `Environment/Level`, but Plane stays at `Environment/Plane`.

| Biome | Plane localPosition | Plane localScale |
|-------|-------------------|-----------------|
| Desert | `(0, 0, 50)` | `(1.5, 1, 10)` |
| Waves | `(0, 0, 23)` | `(1.5, 1, 10)` |
| SnowBlock | varies | `(1.5, 1, 10)` |
| Mountains | multi-section, rotated -15 deg X | `(1.5, 1, 2)` or `(1.5, 1, 3)` per section |
| Roofs | no standard plane — cube geometry for rooftops |
| Metro | no standard plane — cube geometry for tunnel floor |

---

## 6. Spawn Point Placement

### Lanes (X-axis)

| Lane | X |
|------|---|
| Left | `-5` |
| Center | `0` |
| Right | `5` |

### Heights (Y-axis)

| Y | Usage |
|---|-------|
| `2.5` | Ground level |
| `7.5` | Elevated / platform |
| `12.5` | High platform |
| `17.5` | High altitude (Waves) |
| `22.5` | Jump apex coin lines (Waves) |
| `-17.5` | Underground (SnowBlock chains) |
| `-15` | Underground mid (SnowBlock) |

### Depth (Z-axis)

- Range: **0 to 100**
- First spawn: Z >= 10
- Last spawn: Z <= 97.5
- Minimum spacing: **5 units** between spawn points on the same lane
- Distribute across the full range, don't cluster

---

## 7. Enemy Spawn Points

Parent: `Spawnpoints`

### Naming

Use `Spawnpoint (N)` starting at **(0)**. For typed enemies: `SpawnpointRocket (N)`, `SpawnpointStatic (N)`.

### Counts

| Level Type | Enemies |
|------------|---------|
| Easy (diff=0) | 2-4 |
| Hard (diff=1) | 2-3 |
| Exit | 0 |

### Properties

| Property | Notes |
|----------|-------|
| `spawnChance` | 0.25-0.5 for easy, 0.5-1.0 for hard |
| `alwaysSpawn` | false unless guaranteed encounter |
| `allowedType` | diff=0: Static. diff=1: RocketLauncher |

---

## 8. Collectible Spawn Points

Parent: `Collectibles`

### Types

| Type | Name Format | Description |
|------|------------|-------------|
| Coin | `CoinSpawnpoint (N)` | Single coin |
| CoinGroup | `CoinLineSpawnpoint (N)` | Line of coins |
| Magnet | `magnetSpawnpoint` | Magnet powerup |
| Multiplier | `x2Spawnpoint` | 2x score |
| Diamond | `DiamondSpawnpoint` | Rare diamond |
| Fragment | `FragmentCrystalSpawnpoint` | Chain reward |

Number from **(0)**.

### Counts

| Level Type | Collectibles |
|------------|-------------|
| Easy (diff=0) | 2-7 |
| Hard (diff=1) | 2-4 |
| Exit | 2-3 |

### Spawn Chances

| Type | spawnChance |
|------|-----------|
| Coin / CoinGroup | 0.25-1.0 |
| Magnet | 0.15-0.25 (rare), 1.0 (guaranteed in special levels) |
| x2 Multiplier | 0.25-0.5 |
| Diamond | 0.01-0.05 |
| FragmentCrystal | default (chain terminus reward) |

### CoinGroup Settings

| Property | Values |
|----------|--------|
| `groupCount` | 3 or 5 |
| `groupSpacing` | 1.5, 3.0, 4.0, or 5.0 |
| `groupPattern` | Line, Arc, Zigzag, Jump, Vertical |

---

## 9. BiomeData Node Graph

### Node Types

| Type | isStartNode | isEndNode | connections | weight | cooldown |
|------|-------------|-----------|-------------|--------|----------|
| Pool | true | false | empty | 1-2 | 0-3 |
| Chain entry | true | false | → next nodes | 1 | 20 |
| Chain mid | false | false | → next nodes | 1 | 0-1 |
| Chain terminal | false | false | empty | 1 | 0 |
| Exit | false | true | empty | 1 | 0 |

### Chain Rules

- Length: 3-5 segments
- Entry cooldown = 20
- Terminal rewards player (FragmentCrystal, Diamond, generous coins)
- Same prefab can be referenced by multiple nodes for path branching

---

## 10. Biome Reference

### Desert

| Property | Value |
|----------|-------|
| Plane position | `(0, 0, 50)` |
| Y heights | enemies: 2.5, 7.5 — collectibles: 2.5, 7.5, 12.5, 17.5 |
| Pool nodes | weight=2, cooldown=3 |
| Chain | 4 segments, two branching paths |

### Waves

| Property | Value |
|----------|-------|
| Plane position | `(0, 0, 23)` |
| Y heights | enemies: 2.5, 17.5 — collectibles: 2.5, 7.5, 12.5, 17.5, 22.5 |
| Pool nodes | weight=2, cooldown=3 |
| Chain | 4 segments, two paths merging at terminus with FragmentCrystal |

### SnowBlock

| Property | Value |
|----------|-------|
| Y heights | surface: 2.5, 7.5 — underground: -17.5, -15 |
| Pool nodes | weight=2, cooldown=2 |
| Chain | 4 segments (Level 7→8→9→10), underground themed |
| Special | Crate obstacles in underground sections |

### Roofs

| Property | Value |
|----------|-------|
| Ground | Cube geometry, no standard plane |
| Y heights | 2.5, 7.5 |
| Pool nodes | weight=1, cooldown=0 |

### Mountains

| Property | Value |
|----------|-------|
| Ground | Multi-section angled planes (-15 deg X rotation) |
| Y heights | enemies: 7.5, 12.5 — collectibles: 2.5, 5.0, 7.5, 12.5 |

### Metro

| Property | Value |
|----------|-------|
| Ground | Cube geometry tunnel |
| fogDensity | 0.016 (very dense) |
| fogColor | black |
| timeOverride | forced night |
| Special | Ceiling at Y=25, walls at X=+-15 |

---

## 11. Spawn Distribution Patterns

### Spread (Most Common)

```
Z:  10----25----40----55----70----85----95
    E0         C0    E1    C1         C2
```

### Cluster + Gap

```
Z:  10----25----40----55----70----85----95
    E0 C0 E1              C1 E2 C2 C3
```

### Escalating (Chain Levels)

```
Z:  10----25----40----55----70----85----95
    C0         E0    C1    E1 C2 E2 C3
```

### Rules

1. Max 2 spawn points at the same Z — must be on different lanes
2. At least one spawn in first third (Z < 33) and one in last third (Z > 66)

---

## 12. Using the LevelSegmentFactory

### Recommended: Clone from Template (Manual)

1. Open **Tools > Level Segment Factory**
2. Enable "Clone From Template", pick an existing level from the target biome
3. Click "Load Settings From Template"
4. Modify spawn points
5. Enable "Add to Biome Asset", drag in the BiomeData
6. Click "Create Prefab & Add to Biome"

### For AI Agents (Headless API)

Use `LevelSegmentFactory.CreateLevel(LevelDefinition def)`:

1. Build a `LevelDefinition` with biomeName, levelName, spawn definitions
2. The factory auto-resolves the template and BiomeData via `BiomeConfigs`
3. Call `CreateLevel(def)` — it validates, clones template, replaces spawns, saves prefab, registers in biome
4. Returns the saved prefab path, or null on validation failure

Available biome configs: Desert, Waves, SnowBlock, Roofs, Mountains, Metro.

### From JSON File

Use **Tools > Level Segment Factory > Import Level from JSON...** to pick a `.json` file, or call from code:

```csharp
LevelSegmentFactory.CreateLevelFromJson("Assets/Prefabs/Levels/Metro/Level 3.json");
LevelSegmentFactory.CreateLevelsFromFolder("Assets/Prefabs/Levels/Metro/");
```

JSON format uses Unity `JsonUtility` serialization. Enums are integer indices:

| Enum | Values |
|------|--------|
| `enemyType` | 0 = Static, 1 = RocketLauncher |
| `collectibleType` | 0 = Coin, 1 = CoinGroup, 3 = Magnet, 4 = Multiplier, 6 = Diamond, 7 = Fragment |
| `groupPattern` | 0 = Line, 1 = Arc, 2 = Zigzag, 3 = Jump, 4 = Vertical |
| `geometry.type` | 0 = cube, 1 = slope |

Positions use `{"x": 0, "y": 2.5, "z": 50}` format. Rotations use euler angles in the same format.

### Geometry (cubes and slopes)

Levels can define their own geometry primitives in the `geometry` array. Each entry instantiates a cube or slope prefab under `Environment/Level`:

```json
"geometry": [
    {
        "type": 0,
        "position": { "x": 0, "y": 0, "z": 20 },
        "rotation": { "x": 0, "y": 0, "z": 0 }
    }
]
```

- **type**: 0 = cube (`Assets/Models/Prototyping/cube.prefab`), 1 = slope (`Assets/Models/Prototyping/slope.prefab`)
- **position/rotation**: local transform relative to `Environment/Level`. **Scale is always (1,1,1)**—geometry is never resized.
- Cubes have BoxCollider, slopes have convex MeshCollider. Both are Layer 3, tag Ground.
- **Template replacement**: When creating a level from JSON with a `geometry` array, any existing cubes and slopes from the template prefab are **cleared** first. Only the JSON-defined geometry is used—the template is not copied.

See `Assets/Prefabs/Levels/Metro/Level 3.json` for a complete example with geometry.

### Public Static Helpers

| Method | Description |
|--------|-------------|
| `CreateLevel(LevelDefinition)` | Full headless pipeline: validate → clone → spawn → save → register |
| `CreateLevelFromJson(jsonPath)` | Load a LevelDefinition from JSON and create the level |
| `CreateLevelsFromFolder(folderPath)` | Batch-create all levels from JSON files in a folder |
| `Validate(LevelDefinition)` | Returns list of rule violations (empty = valid) |
| `RegisterInBiome(biome, segment, ...)` | Add a segment as a node in a BiomeData asset |
| `CreateEnemySpawn(parent, name, pos, ...)` | Create an EnemySpawnPoint child |
| `CreateCollectibleSpawn(parent, name, pos, ...)` | Create a CollectibleSpawnPoint child |
| `CreateObstacleSpawn(parent, name, pos, ...)` | Create an ObstacleSpawnPoint child |
| `PlaceGeometry(root, geometryList)` | Instantiate cubes/slopes under Environment/Level |
| `ClearChildren(transform)` | Remove all children of a transform |
| `ApplySegmentFields(segment, ...)` | Set LevelSegment properties via SerializedObject |
| `ApplyRevealFields(reveal, ...)` | Set SegmentReveal properties via SerializedObject |

---

## 13. Validation Checklist

- [ ] Root has `LevelSegment` and `SegmentReveal`
- [ ] `SegmentReveal.visualRoot` → Environment transform
- [ ] `segmentLength` = 100
- [ ] All enemy spawns are children of `Spawnpoints`
- [ ] All collectible spawns are children of `Collectibles`
- [ ] `Environment/Level` child exists, all cubes and slopes are inside it
- [ ] Plane (if present) is a direct child of `Environment`, not inside `Level`
- [ ] Spawn X positions: only -5, 0, or 5
- [ ] Spawn Y positions: valid height tier
- [ ] Spawn Z positions: 0-100, min 5 unit spacing
- [ ] Enemy count matches difficulty
- [ ] At least one collectible exists
- [ ] `CollectSpawnPoints()` called
- [ ] Prefab saved in `Assets/Prefabs/Levels/{BiomeName}/`
- [ ] Node registered in BiomeData

---

## 14. Quick Reference

```
Segment Length:     100
Root Position:      (0, 0, -10)
Environment:        (0, 0, 27)
Environment/Level:  (0, 0, 0) — cubes and slopes only
Plane:              direct child of Environment (not in Level)
Background:         (0, 0, -17) relative to Environment
Lanes:              X = -5, 0, 5
Heights:            Y = 2.5, 7.5, 12.5, 17.5, 22.5
Z Range:            10 to 97.5
Min Z Spacing:      5 units
Numbering:          start at (0)
Enemy Count:        2-4 (easy), 2-3 (hard), 0 (exit)
Collectible Count:  2-7
Pool Node:          startNode=true, weight=1-2, cooldown=0-3
Chain Entry:        startNode=true, weight=1, cooldown=20
Exit Node:          endNode=true, 0 enemies
```
