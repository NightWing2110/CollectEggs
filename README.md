# CollectEggs

## 1. Overview

CollectEggs is a game where multiple players compete to collect randomly colored eggs before the match timer ends.

One player is controlled by the keyboard. The remaining players are represented by bot/remote-player actors with custom AI and custom pathfinding. The project is structured around a local in-process server simulation controller so the game can later move toward a real server with less refactoring.

## 2. Unity Version & Platform

- Unity version: `2021.3.45`
- Target platform: PC
- Input: keyboard
- Camera style: top-down/isometric-style 3D view

## 3. How to Run

1. Open the project in Unity.
2. Open `Assets/Scenes/GamePlayScene.unity`.
3. Press Play.
4. Move the local player and collect eggs until the timer ends.
5. Use the result panel to restart the match.

## 4. Controls

- Move: `WASD` or arrow keys.
- Collect egg: move close enough to touch an egg.

## 5. Gameplay Rules

- The match starts with `ServerConfig.PlayerCount` players.
- The local player (You) is controlled by keyboard input.
- The other players are bot/remote-player actors.
- Eggs spawn at random valid positions on the map.
- Players collect eggs by touching them.
- Each collected egg adds score to the collecting player.
- The player with the highest score when the timer reaches zero wins. If multiple players have the same score, they share the same rank.

## 6. Configuration

Main match settings are stored in `ServerConfig` and serialized through `GameBootstrapper`.

- `PlayerCount`: code constant controlling the number of players.
- `matchDurationSeconds`: match length.
- `playerMoveSpeed`: player movement speed.
- `eggCollectRadius`: server collect radius.
- `eggCollectValidationSlack`: tolerance for collect validation.
- `initialEggCount`: initial egg count.
- `snapshotIntervalMinSeconds` / `snapshotIntervalMaxSeconds`: random server snapshot interval.
- `simulatedTransportLatencyMinSeconds` / `simulatedTransportLatencyMaxSeconds`: simulated network delay for messages.

The default configuration keeps random snapshot updates and simulated latency enabled. To test without artificial network delay, set `simulatedTransportLatencyMinSeconds` and `simulatedTransportLatencyMaxSeconds` to `0`; snapshot intervals remain configurable separately.

## 7. Architecture Overview

The project separates client-side presentation/control from server-side simulation as much as the current prototype allows.

- `Core`: match lifecycle and scene wiring.
- `Client`: receives server messages and updates scene views.
- `Server`: in-process server simulation controller, server state, snapshot builder, and spawn/movement resolver.
- `Shared`: messages, snapshots, and shared data contracts.
- `Gameplay`: movement, egg collection requests, timer, scoring, spawning, and entities.
- `Bots`: client-side bot AI and custom grid pathfinding.
- `Networking`: simulated transport and latency queue.
- `UI`: HUD and result panel.

## 8. Server Simulation Controller / Client

Server responsibilities:

- Creates match players from `ServerConfig.PlayerCount`.
- Sends `MatchStartedMessage`.
- Receives `PlayerInputMessage` for the local player.
- Integrates local player movement into `ServerPlayerState`.
- Decides egg spawn, collect validation, score updates, and respawn.
- Sends `GameStateSnapshotMessage` at random intervals.
- Sends `MatchEndedMessage` with final scores and winner ids when match time reaches zero.
- Rejects late egg collect requests after match time ends.

Client responsibilities:

- Spawns and owns the Unity scene views for players, bots, eggs, HUD, and result UI.
- Reads keyboard input for the local player and sends `PlayerInputMessage` to the server simulation controller.
- Sends `EggCollectRequestMessage` when the local player or client-side bots request an egg collect.
- Receives `MatchStartedMessage`, `GameStateSnapshotMessage`, and `MatchEndedMessage`.
- Applies server-approved egg, score, timer, and local player position updates from snapshots.
- Runs bot AI locally in the current prototype, which is why bot positions are not authoritative server state yet.
- Delegates match setup and snapshot application through small client-side helpers instead of keeping all client logic inside `ClientGameController`.


The server movement resolver uses `IServerWorldQuery`, so server-side spawn/movement logic is not directly tied to scene objects. The current adapter is `PhysicsServerWorldQuery`, which uses Unity physics for the local simulator path.

## 9. Message System

Client and server simulation controller communicate through message contracts instead of direct gameplay calls.

- `MatchStartedMessage`: initial players, eggs, and match rules.
- `PlayerInputMessage`: local player movement input and input sequence.
- `EggCollectRequestMessage`: client request to collect an egg.
- `GameStateSnapshotMessage`: authoritative player positions, egg states, scores, remaining time, and rules.
- `MatchEndedMessage`: final server-approved scores and winner player ids when the match ends.

Egg collect/spawn render updates are snapshot-driven. There are no immediate dedicated egg collected/spawned render messages in the current flow.

## 10. Bot AI & Pathfinding

Bots use custom AI through `BotController`.

- Selects egg targets.
- Uses custom grid-based A* pathfinding.
- Avoids map obstacles/blockers through grid and layer sampling.
- Repaths over time.
- Includes simple stuck detection and recovery.
- Splits bot responsibilities across focused partial classes and helpers for target selection, path state, collection handling, stuck recovery, and path planning.

No external pathfinding library/plugin is used.

Current limitation: bot movement still runs on the client.

Because bot positions are still client-driven, bot collect requests currently include the bot's client-side position for server validation. This is a prototype bridge until bot movement and bot collect checks move fully into server state.

## 11. Interpolation & Latency Simulation

The server simulation controller sends snapshots at random intervals controlled by:

- `snapshotIntervalMinSeconds`
- `snapshotIntervalMaxSeconds`

The simulated network delay is controlled by:

- `simulatedTransportLatencyMinSeconds`
- `simulatedTransportLatencyMaxSeconds`

`SimulatedTransport` applies latency to both client-to-server and server-to-client messages.

The current default latency range is enabled intentionally. Set both simulated latency values to `0` only when testing a no-delay local run.

Local player latency handling is implemented through input sequence tracking and reconciliation. The `enableClientPrediction` option can move the local player immediately before the server snapshot arrives. When snapshots arrive, the client uses `PlayerSnapshot.LastProcessedInputSequence` to drop confirmed inputs and can replay any inputs the server has not processed yet.

When a snapshot arrives, the client reconciles the local position with the server-approved position. The remaining improvement is to enable and polish smooth prediction correction so late snapshots or larger position differences do not cause visible pops.

Remote-player interpolation is not implemented yet. Bots are still driven locally on the client, so they are not rendered from interpolated server-owned snapshots. Local-player prediction/reconciliation is a separate input-latency feature

## 12. Map & Obstacles

The map contains obstacles and blockers.

- Obstacles use Unity colliders/layers.
- Bot pathfinding samples the map through `GridMap`.
- Server movement validation checks blocking colliders through `IServerWorldQuery`.
- Egg spawn validation avoids blockers, players, and occupied egg positions.

Egg colliders are triggers so eggs can be collected without physically blocking the player while waiting for delayed server snapshots.

## 13. Scoring & Win Condition

- Score is updated only from server-approved state.
- `ServerSimulationController` updates `ServerPlayerState.Score`.
- `GameStateSnapshotMessage` sends score snapshots.
- Client UI mirrors score from snapshots.
- Players with the same score share the same rank. For example, three players tied for first are all shown as `#1`, and the next lower score is shown as `#2`.

## 14. Project Structure

```text
Assets/Scripts
├── Bots
├── Client
├── Core
├── Gameplay
├── Networking
├── Server
├── Shared
└── UI
```

Important files:

- `Assets/Scripts/Core/GameBootstrapper.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/GameSceneContext.cs`
- `Assets/Scripts/Server/Simulation/ServerSimulationController.cs`
- `Assets/Scripts/Server/Simulation/ServerConfig.cs`
- `Assets/Scripts/Networking/Transport/SimulatedTransport.cs`
- `Assets/Scripts/Client/ClientGameController.cs`
- `Assets/Scripts/Bots/BotController.cs`
- `Assets/Scripts/Bots/EggApproachPathPlanner.cs`
- `Assets/Scripts/Gameplay/Navigation/GridMap.cs`

## 15. Completed Features

- Keyboard-controlled local player.
- Multiple players controlled by `ServerConfig.PlayerCount`.
- Bot AI with custom pathfinding.
- Map obstacles/blockers.
- Random colored eggs.
- Match timer.
- Result UI and restart button.
- Server simulation controller module.
- Message/snapshot contracts.
- Server-driven egg spawn, collect validation, score, and respawn.
- Server-driven match end message with final scores and winners.
- Random snapshot interval support.
- Simulated network latency support.

## 16. Unfinished / Partially Finished Features

- True remote-player interpolation from server snapshots is not implemented yet.
- There is no snapshot buffer yet for rendering remote players between server snapshots.
- Bot/remote players still run client-side movement through `BotController`, so their movement is not server-authoritative yet.
- Bot positions are not authoritative server state.
- Bot collect validation still depends on bot positions reported by the client.
- Local player prediction/reconciliation has groundwork, but `enableClientPrediction` is disabled by default in the prefab.
- Smooth correction under high latency still needs polish.
- Real network transport is not implemented yet; the project currently uses simulated transport.

## 17. Planned Solutions for Unfinished Features

- Move bot/remote-player positions into server state.
- Make the server simulation controller update all player positions, not only the local player.
- Send all player positions in snapshots.
- Add snapshot buffering and interpolation for non-local players.
- Improve local player reconciliation with smooth correction and snap thresholds.
- Keep client-side prediction only for the local keyboard player.
- Replace `SimulatedTransport` with a real transport implementation behind `IGameTransport`.

Planned approach for moving bots to the server:

- Add server-owned `MapData` so bot pathfinding can run from `MapData` instead of reading Unity scene objects.
- Move bot movement simulation out of client `Update()` and into server state.
- Add bot state for current target egg, waypoint/path progress, blocked movement, collect range checks, and retargeting when an egg is stolen or pathing fails.
- Let the server update bot positions and include those positions in snapshots.
- Stop sending bot collect requests from the client. The server should check bot-to-egg distance and decide collection directly.
- Keep the client responsible only for rendering bot objects from server snapshots.

## 18. Remaining Bugs

- The local player can pass through bots. Bot movement currently runs on the client, while local player movement is resolved by the server simulation controller. Because the server does not own the real-time bot positions, it cannot use bots as reliable blockers for local player movement.
- Bot-to-bot blocking may still appear to work because those actors are moved by client-side Unity `CharacterController` physics. This is different from the local player path, which follows server-approved positions from snapshots.

## 19. Future Improvements

- Server-authoritative bot simulation.
- Remote-player interpolation from snapshot buffers.
- Smooth local reconciliation under high latency.
- Real network transport implementation.
- Better debug UI for latency, queued messages, and snapshot timing.
- More map layouts and obstacle patterns.