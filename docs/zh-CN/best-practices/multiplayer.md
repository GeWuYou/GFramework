# 多人游戏架构指南

> 基于 GFramework 架构设计高性能、可扩展的多人游戏系统。

## 📋 目录

- [概述](#概述)
- [核心概念](#核心概念)
- [架构设计](#架构设计)
- [状态管理](#状态管理)
- [命令模式与输入处理](#命令模式与输入处理)
- [事件同步](#事件同步)
- [网络优化](#网络优化)
- [安全考虑](#安全考虑)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

## 概述

多人游戏开发面临着单机游戏所没有的独特挑战:

### 主要挑战

1. **网络延迟** - 玩家操作和服务器响应之间存在不可避免的延迟
2. **状态同步** - 确保所有客户端看到一致的游戏状态
3. **带宽限制** - 需要高效传输游戏数据,避免网络拥塞
4. **作弊防护** - 防止客户端篡改游戏逻辑和数据
5. **并发处理** - 同时处理多个玩家的输入和状态更新
6. **断线重连** - 优雅处理网络中断和玩家重新连接

### GFramework 的优势

GFramework 的架构设计天然适合多人游戏开发:

- **分层架构** - 清晰分离客户端逻辑、网络层和服务器逻辑
- **事件系统** - 松耦合的事件驱动架构便于状态同步
- **命令模式** - 统一的输入处理和验证机制
- **Model-System 分离** - 数据和逻辑分离便于状态管理
- **模块化设计** - 网络功能可以作为独立模块集成

## 核心概念

### 1. 客户端-服务器架构

```csharp
// 服务器架构
public class ServerArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册服务器专用的 Model
        RegisterModel(new ServerGameStateModel());
        RegisterModel(new PlayerConnectionModel());

        // 注册服务器专用的 System
        RegisterSystem(new ServerNetworkSystem());
        RegisterSystem(new AuthorityGameLogicSystem());
        RegisterSystem(new StateReplicationSystem());
        RegisterSystem(new AntiCheatSystem());

        // 注册工具
        RegisterUtility(new NetworkUtility());
        RegisterUtility(new ValidationUtility());
    }
}

// 客户端架构
public class ClientArchitecture : Architecture
{
    protected override void Init()
    {
        // 注册客户端专用的 Model
        RegisterModel(new ClientGameStateModel());
        RegisterModel(new PredictionModel());

        // 注册客户端专用的 System
        RegisterSystem(new ClientNetworkSystem());
        RegisterSystem(new PredictionSystem());
        RegisterSystem(new InterpolationSystem());
        RegisterSystem(new ClientInputSystem());

        // 注册工具
        RegisterUtility(new NetworkUtility());
    }
}
```

### 2. 状态同步策略

#### 状态同步 (State Synchronization)

服务器定期向客户端发送完整的游戏状态。

```csharp
// 游戏状态快照
public struct GameStateSnapshot
{
    public uint Tick { get; set; }
    public long Timestamp { get; set; }
    public PlayerState[] Players { get; set; }
    public EntityState[] Entities { get; set; }
}

public struct PlayerState
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public int Health { get; set; }
    public PlayerAnimationState AnimationState { get; set; }
}

// 状态复制系统
public class StateReplicationSystem : AbstractSystem
{
    private ServerGameStateModel _gameState;
    private PlayerConnectionModel _connections;
    private uint _currentTick;

    protected override void OnInit()
    {
        _gameState = this.GetModel&lt;ServerGameStateModel&gt;();
        _connections = this.GetModel&lt;PlayerConnectionModel&gt;();

        // 每个 tick 复制状态
        this.RegisterEvent&lt;ServerTickEvent&gt;(OnServerTick);
    }

    private void OnServerTick(ServerTickEvent e)
    {
        _currentTick++;

        // 创建状态快照
        var snapshot = CreateSnapshot();

        // 发送给所有连接的客户端
        foreach (var connection in _connections.ActiveConnections)
        {
            SendSnapshotToClient(connection, snapshot);
        }
    }

    private GameStateSnapshot CreateSnapshot()
    {
        return new GameStateSnapshot
        {
            Tick = _currentTick,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Players = _gameState.Players.Select(p => new PlayerState
            {
                PlayerId = p.Id,
                Position = p.Position,
                Rotation = p.Rotation,
                Health = p.Health,
                AnimationState = p.AnimationState
            }).ToArray(),
            Entities = _gameState.Entities.Select(e => CreateEntityState(e)).ToArray()
        };
    }
}
```

#### 增量同步 (Delta Synchronization)

只发送状态变化,减少带宽消耗。

```csharp
// 增量状态
public struct DeltaState
{
    public uint Tick { get; set; }
    public uint BaseTick { get; set; }
    public PlayerDelta[] PlayerDeltas { get; set; }
    public EntityDelta[] EntityDeltas { get; set; }
}

public struct PlayerDelta
{
    public string PlayerId { get; set; }
    public DeltaFlags Flags { get; set; }
    public Vector3? Position { get; set; }
    public Quaternion? Rotation { get; set; }
    public int? Health { get; set; }
}

[Flags]
public enum DeltaFlags
{
    None = 0,
    Position = 1 &lt;&lt; 0,
    Rotation = 1 &lt;&lt; 1,
    Health = 1 &lt;&lt; 2,
    Animation = 1 &lt;&lt; 3
}

// 增量复制系统
public class DeltaReplicationSystem : AbstractSystem
{
    private readonly Dictionary&lt;uint, GameStateSnapshot&gt; _snapshotHistory = new();
    private const int MaxHistorySize = 60; // 保留 60 个快照

    protected override void OnInit()
    {
        this.RegisterEvent&lt;ServerTickEvent&gt;(OnServerTick);
    }

    private void OnServerTick(ServerTickEvent e)
    {
        var currentSnapshot = CreateSnapshot();
        _snapshotHistory[e.Tick] = currentSnapshot;

        // 清理旧快照
        CleanupOldSnapshots(e.Tick);

        // 为每个客户端生成增量
        foreach (var connection in GetConnections())
        {
            var lastAckedTick = connection.LastAcknowledgedTick;

            if (_snapshotHistory.TryGetValue(lastAckedTick, out var baseSnapshot))
            {
                var delta = CreateDelta(baseSnapshot, currentSnapshot);
                SendDeltaToClient(connection, delta);
            }
            else
            {
                // 客户端太落后,发送完整快照
                SendSnapshotToClient(connection, currentSnapshot);
            }
        }
    }

    private DeltaState CreateDelta(GameStateSnapshot baseSnapshot, GameStateSnapshot currentSnapshot)
    {
        var delta = new DeltaState
        {
            Tick = currentSnapshot.Tick,
            BaseTick = baseSnapshot.Tick,
            PlayerDeltas = new List&lt;PlayerDelta&gt;()
        };

        // 比较玩家状态
        foreach (var currentPlayer in currentSnapshot.Players)
        {
            var basePlayer = baseSnapshot.Players.FirstOrDefault(p => p.PlayerId == currentPlayer.PlayerId);

            if (basePlayer.PlayerId == null)
            {
                // 新玩家,发送完整状态
                delta.PlayerDeltas.Add(CreateFullPlayerDelta(currentPlayer));
            }
            else
            {
                // 计算差异
                var playerDelta = CreatePlayerDelta(basePlayer, currentPlayer);
                if (playerDelta.Flags != DeltaFlags.None)
                {
                    delta.PlayerDeltas.Add(playerDelta);
                }
            }
        }

        return delta;
    }

    private PlayerDelta CreatePlayerDelta(PlayerState baseState, PlayerState currentState)
    {
        var delta = new PlayerDelta { PlayerId = currentState.PlayerId };

        if (Vector3.Distance(baseState.Position, currentState.Position) &gt; 0.01f)
        {
            delta.Flags |= DeltaFlags.Position;
            delta.Position = currentState.Position;
        }

        if (Quaternion.Angle(baseState.Rotation, currentState.Rotation) &gt; 0.1f)
        {
            delta.Flags |= DeltaFlags.Rotation;
            delta.Rotation = currentState.Rotation;
        }

        if (baseState.Health != currentState.Health)
        {
            delta.Flags |= DeltaFlags.Health;
            delta.Health = currentState.Health;
        }

        return delta;
    }
}
```

### 3. 客户端预测与回滚

客户端立即响应玩家输入,然后在收到服务器确认后进行校正。

```csharp
// 输入命令
public struct PlayerInputCommand
{
    public uint Tick { get; set; }
    public long Timestamp { get; set; }
    public Vector2 MoveDirection { get; set; }
    public Vector2 LookDirection { get; set; }
    public InputFlags Flags { get; set; }
}

[Flags]
public enum InputFlags
{
    None = 0,
    Jump = 1 &lt;&lt; 0,
    Attack = 1 &lt;&lt; 1,
    Interact = 1 &lt;&lt; 2,
    Reload = 1 &lt;&lt; 3
}

// 客户端预测系统
public class ClientPredictionSystem : AbstractSystem
{
    private PredictionModel _prediction;
    private ClientGameStateModel _gameState;
    private readonly Queue&lt;PlayerInputCommand&gt; _pendingInputs = new();
    private uint _lastProcessedServerTick;

    protected override void OnInit()
    {
        _prediction = this.GetModel&lt;PredictionModel&gt;();
        _gameState = this.GetModel&lt;ClientGameStateModel&gt;();

        this.RegisterEvent&lt;PlayerInputEvent&gt;(OnPlayerInput);
        this.RegisterEvent&lt;ServerStateReceivedEvent&gt;(OnServerStateReceived);
    }

    private void OnPlayerInput(PlayerInputEvent e)
    {
        var input = new PlayerInputCommand
        {
            Tick = _prediction.CurrentTick,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            MoveDirection = e.MoveDirection,
            LookDirection = e.LookDirection,
            Flags = e.Flags
        };

        // 保存输入用于重放
        _pendingInputs.Enqueue(input);

        // 立即应用预测
        ApplyInput(input);

        // 发送到服务器
        SendInputToServer(input);
    }

    private void OnServerStateReceived(ServerStateReceivedEvent e)
    {
        _lastProcessedServerTick = e.Snapshot.Tick;

        // 应用服务器状态
        ApplyServerState(e.Snapshot);

        // 移除已确认的输入
        while (_pendingInputs.Count &gt; 0 && _pendingInputs.Peek().Tick &lt;= e.Snapshot.Tick)
        {
            _pendingInputs.Dequeue();
        }

        // 重放未确认的输入
        ReplayPendingInputs();
    }

    private void ApplyInput(PlayerInputCommand input)
    {
        var player = _gameState.LocalPlayer;

        // 应用移动
        var movement = input.MoveDirection * player.Speed * Time.DeltaTime;
        player.Position += new Vector3(movement.X, 0, movement.Y);

        // 应用旋转
        if (input.LookDirection != Vector2.Zero)
        {
            player.Rotation = Quaternion.LookRotation(
                new Vector3(input.LookDirection.X, 0, input.LookDirection.Y)
            );
        }

        // 应用动作
        if ((input.Flags &amp; InputFlags.Jump) != 0 &amp;&amp; player.IsGrounded)
        {
            player.Velocity = new Vector3(player.Velocity.X, player.JumpForce, player.Velocity.Z);
        }
    }

    private void ReplayPendingInputs()
    {
        // 从服务器状态开始重放所有未确认的输入
        var savedState = SavePlayerState();

        foreach (var input in _pendingInputs)
        {
            ApplyInput(input);
        }
    }
}
```

## 架构设计

### 1. 分离逻辑层

```csharp
// 共享游戏逻辑 (客户端和服务器都使用)
public class SharedGameLogic
{
    public static void ProcessMovement(PlayerState player, Vector2 moveDirection, float deltaTime)
    {
        var movement = moveDirection.Normalized() * player.Speed * deltaTime;
        player.Position += new Vector3(movement.X, 0, movement.Y);
    }

    public static bool CanJump(PlayerState player)
    {
        return player.IsGrounded &amp;&amp; !player.IsStunned;
    }

    public static int CalculateDamage(int attackPower, int defense, float criticalChance)
    {
        var baseDamage = Math.Max(1, attackPower - defense);
        var isCritical = Random.Shared.NextDouble() &lt; criticalChance;
        return isCritical ? baseDamage * 2 : baseDamage;
    }
}

// 服务器权威逻辑
public class ServerGameLogicSystem : AbstractSystem
{
    private ServerGameStateModel _gameState;

    protected override void OnInit()
    {
        _gameState = this.GetModel&lt;ServerGameStateModel&gt;();

        this.RegisterEvent&lt;PlayerInputReceivedEvent&gt;(OnPlayerInputReceived);
        this.RegisterEvent&lt;AttackRequestEvent&gt;(OnAttackRequest);
    }

    private void OnPlayerInputReceived(PlayerInputReceivedEvent e)
    {
        var player = _gameState.GetPlayer(e.PlayerId);

        // 验证输入
        if (!ValidateInput(e.Input))
        {
            SendInputRejection(e.PlayerId, "Invalid input");
            return;
        }

        // 应用共享逻辑
        SharedGameLogic.ProcessMovement(player, e.Input.MoveDirection, Time.DeltaTime);

        // 服务器端验证
        if ((e.Input.Flags &amp; InputFlags.Jump) != 0)
        {
            if (SharedGameLogic.CanJump(player))
            {
                player.Velocity = new Vector3(player.Velocity.X, player.JumpForce, player.Velocity.Z);
            }
        }
    }

    private void OnAttackRequest(AttackRequestEvent e)
    {
        var attacker = _gameState.GetPlayer(e.AttackerId);
        var target = _gameState.GetPlayer(e.TargetId);

        // 服务器端验证
        if (!CanAttack(attacker, target))
        {
            return;
        }

        // 计算伤害
        var damage = SharedGameLogic.CalculateDamage(
            attacker.AttackPower,
            target.Defense,
            attacker.CriticalChance
        );

        // 应用伤害
        target.Health = Math.Max(0, target.Health - damage);

        // 广播事件
        this.SendEvent(new PlayerDamagedEvent
        {
            AttackerId = e.AttackerId,
            TargetId = e.TargetId,
            Damage = damage,
            RemainingHealth = target.Health
        });

        if (target.Health == 0)
        {
            this.SendEvent(new PlayerDiedEvent
            {
                PlayerId = e.TargetId,
                KillerId = e.AttackerId
            });
        }
    }
}

// 客户端表现逻辑
public class ClientPresentationSystem : AbstractSystem
{
    protected override void OnInit()
    {
        this.RegisterEvent&lt;PlayerDamagedEvent&gt;(OnPlayerDamaged);
        this.RegisterEvent&lt;PlayerDiedEvent&gt;(OnPlayerDied);
    }

    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        // 播放受击特效
        PlayDamageEffect(e.TargetId, e.Damage);

        // 播放受击音效
        PlayDamageSound(e.TargetId);

        // 更新 UI
        UpdateHealthBar(e.TargetId, e.RemainingHealth);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        // 播放死亡动画
        PlayDeathAnimation(e.PlayerId);

        // 播放死亡音效
        PlayDeathSound(e.PlayerId);

        // 显示击杀提示
        ShowKillFeed(e.KillerId, e.PlayerId);
    }
}
