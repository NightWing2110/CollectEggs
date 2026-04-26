# CollectEggs

## 1. Tổng Quan

CollectEggs là trò chơi nơi nhiều người chơi cạnh tranh thu thập trứng màu ngẫu nhiên trước khi thời gian trận đấu kết thúc.

Một người chơi được điều khiển bằng bàn phím. Các người chơi còn lại được biểu diễn bằng bot/remote-player có AI và pathfinding tự viết. Dự án được tổ chức quanh một server simulation controller chạy trong client để sau này có thể chuyển sang server thật với ít thay đổi hơn.

## 2. Phiên Bản Unity & Nền Tảng

- Phiên bản Unity: `2021.3.45`
- Nền tảng mục tiêu: PC
- Input: bàn phím
- Góc nhìn: top-down/isometric-style 3D

## 3. Cách Chạy

1. Mở project trong Unity.
2. Mở `Assets/Scenes/GamePlayScene.unity`.
3. Nhấn Play.
4. Di chuyển local player và thu thập trứng cho đến khi hết giờ.
5. Dùng bảng kết quả để restart trận đấu.

## 4. Điều Khiển

- Di chuyển: `WASD` hoặc phím mũi tên.
- Thu thập trứng: di chuyển chạm vào trứng để thu thập.

## 5. Luật Chơi

- Trận đấu bắt đầu với số lượng player từ `ServerConfig.PlayerCount`.
- Local player (You) được điều khiển bằng bàn phím.
- Các player còn lại là bot/remote-player.
- Trứng xuất hiện ở vị trí hợp lệ ngẫu nhiên trên map.
- Player thu thập trứng bằng cách chạm vào những quả trứng.
- Mỗi trứng được thu thập sẽ cộng điểm cho player tương ứng.
- Player có điểm cao nhất khi hết giờ là người chiến thắng. Nếu nhiều player bằng điểm nhau, các player đó sẽ cùng thứ hạng.

## 6. Cấu Hình

Các cấu hình chính của trận đấu nằm trong `ServerConfig` và được serialize thông qua `GameBootstrapper`.

- `PlayerCount`: hằng số trong code quyết định số lượng player.
- `matchDurationSeconds`: thời lượng trận đấu.
- `playerMoveSpeed`: tốc độ di chuyển player.
- `eggCollectRadius`: bán kính collect phía server.
- `eggCollectValidationSlack`: độ nới cho validate collect.
- `initialEggCount`: số lượng trứng ban đầu.
- `snapshotIntervalMinSeconds` / `snapshotIntervalMaxSeconds`: khoảng thời gian ngẫu nhiên giữa các snapshot server.
- `simulatedTransportLatencyMinSeconds` / `simulatedTransportLatencyMaxSeconds`: độ trễ mạng giả lập cho message.

Cấu hình mặc định đang bật random snapshot update và simulated latency. Nếu muốn test không có độ trễ nhân tạo, set `simulatedTransportLatencyMinSeconds` và `simulatedTransportLatencyMaxSeconds` về `0`; snapshot interval được cấu hình riêng.

## 7. Tổng Quan Kiến Trúc

Dự án tách client-side presentation/control khỏi server-side simulation nhiều nhất có thể trong phạm vi prototype hiện tại.

- `Core`: vòng đời trận đấu và liên kết scene.
- `Client`: nhận message từ server và cập nhật view trong scene.
- `Server`: server simulation controller trong client, server state, snapshot builder, spawn/movement resolver.
- `Shared`: message, snapshot, shared data contracts.
- `Gameplay`: movement, egg collect request, timer, scoring, spawning, entities.
- `Bots`: bot AI phía client và grid pathfinding tự viết.
- `Networking`: simulated transport và latency queue.
- `UI`: HUD và result panel.

## 8. Server Simulation Controller / Client

Các trách nhiệm server đang sở hữu:

- Tạo player theo `ServerConfig.PlayerCount`.
- Gửi `MatchStartedMessage`.
- Nhận `PlayerInputMessage` cho local player.
- Tích hợp movement của local player vào `ServerPlayerState`.
- Quyết định egg spawn, validate collect, cập nhật score, và respawn.
- Gửi `GameStateSnapshotMessage` theo khoảng thời gian ngẫu nhiên.
- Gửi `MatchEndedMessage` với final scores và winner ids khi thời gian trận đấu về 0.
- Từ chối collect request đến trễ sau khi trận đấu đã kết thúc.

Các trách nhiệm phía client:

- Spawn và sở hữu Unity scene views cho player, bot, egg, HUD, và result UI.
- Đọc keyboard input của local player và gửi `PlayerInputMessage` lên server simulation controller.
- Gửi `EggCollectRequestMessage` khi local player hoặc bot phía client request collect egg.
- Nhận `MatchStartedMessage`, `GameStateSnapshotMessage`, và `MatchEndedMessage`.
- Apply egg, score, timer, và vị trí local player đã được server xác nhận từ snapshots.
- Chạy bot AI ở client trong prototype hiện tại, vì vậy bot position chưa phải authoritative server state.
- Tách match setup và snapshot application sang các helper nhỏ phía client thay vì dồn toàn bộ logic vào `ClientGameController`.

Server movement resolver dùng `IServerWorldQuery`, nên logic spawn/movement phía server không phụ thuộc trực tiếp vào scene object. Adapter hiện tại là `PhysicsServerWorldQuery`, dùng Unity physics cho đường chạy local simulator.

## 9. Hệ Thống Message

Client và server simulation controller giao tiếp bằng message contracts thay vì gọi trực tiếp logic gameplay.

- `MatchStartedMessage`: player, egg, và rule ban đầu.
- `PlayerInputMessage`: input di chuyển của local player và input sequence.
- `EggCollectRequestMessage`: request collect egg từ client.
- `GameStateSnapshotMessage`: authoritative player positions, egg states, scores, remaining time, và rules.
- `MatchEndedMessage`: final scores và winner player ids đã được server xác nhận khi trận đấu kết thúc.

Egg collect/spawn render update hiện đi qua snapshot. Luồng hiện tại không dùng message riêng tức thời cho egg collected/spawned để render.

## 10. Bot AI & Pathfinding

Bot dùng AI tự viết thông qua `BotController`.

- Chọn target egg.
- Dùng grid-based A* pathfinding tự viết.
- Tránh obstacle/blocker trên map thông qua grid và layer sampling.
- Repath theo thời gian.
- Có stuck detection và recovery đơn giản.
- Tách trách nhiệm bot qua các partial class và helper nhỏ cho target selection, path state, collection handling, stuck recovery, và path planning.

Không dùng pathfinding library/plugin bên ngoài.

Giới hạn hiện tại: bot movement vẫn chạy ở client.

Vì bot position hiện vẫn do client điều khiển, bot collect request tạm thời gửi kèm vị trí bot phía client để server validate. Đây là bước trung gian của prototype cho đến khi bot movement và bot collect check được chuyển hoàn toàn vào server state.

## 11. Interpolation & Giả Lập Latency

Server simulation controller gửi snapshot theo khoảng thời gian ngẫu nhiên được điều khiển bởi:

- `snapshotIntervalMinSeconds`
- `snapshotIntervalMaxSeconds`

Độ trễ mạng giả lập được điều khiển bởi:

- `simulatedTransportLatencyMinSeconds`
- `simulatedTransportLatencyMaxSeconds`

`SimulatedTransport` áp dụng latency cho cả client-to-server và server-to-client messages.

Khoảng latency mặc định hiện được bật có chủ đích. Chỉ set cả hai giá trị simulated latency về `0` khi muốn test local run không có delay.

Local player hiện xử lý latency bằng input sequence tracking và reconciliation. Tuỳ chọn `enableClientPrediction` có thể cho local player di chuyển ngay trước khi snapshot server về. Khi snapshot về, client dùng `PlayerSnapshot.LastProcessedInputSequence` để bỏ các input server đã xác nhận và có thể replay các input server chưa xử lý.

Khi snapshot về, client reconcile vị trí local player với vị trí đã được server xác nhận. Phần còn cần cải thiện là bật và polish smooth prediction correction để snapshot về trễ hoặc lệch nhiều không làm nhân vật bị giật rõ.

Remote-player interpolation hiện chưa được implement. Bot vẫn đang được điều khiển local ở client, nên chưa render từ các server-owned snapshots đã được nội suy. Local-player prediction/reconciliation là phần riêng để xử lý input latency.

## 12. Map & Obstacles

Map có obstacles và blockers.

- Obstacles dùng Unity colliders/layers.
- Bot pathfinding sample map qua `GridMap`.
- Server movement validation kiểm tra blocking colliders qua `IServerWorldQuery`.
- Egg spawn validation tránh blocker, player, và vị trí trứng đang chiếm.

Egg collider là trigger để trứng có thể được collect mà không chặn vật lý player trong lúc chờ delayed server snapshot.

## 13. Scoring & Điều Kiện Thắng

- Score chỉ cập nhật từ server-approved state.
- `ServerSimulationController` cập nhật `ServerPlayerState.Score`.
- `GameStateSnapshotMessage` gửi score snapshots.
- UI phía client mirror score từ snapshot.
- Các player bằng điểm sẽ dùng chung cùng một rank. Ví dụ ba player cùng cao điểm nhất đều là `#1`, score thấp tiếp theo sẽ là `#2`.

## 14. Cấu Trúc Project

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

Các file quan trọng:

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

## 15. Tính Năng Đã Hoàn Thành

- Local player điều khiển bằng bàn phím.
- Nhiều player theo `ServerConfig.PlayerCount`.
- Bot AI với pathfinding tự viết.
- Map có obstacles/blockers.
- Trứng màu ngẫu nhiên.
- Match timer.
- Result UI và restart button.
- Server simulation controller module.
- Message/snapshot contracts.
- Egg spawn, collect validation, score, và respawn do server quyết định.
- Match end message với final scores và winners do server gửi.
- Hỗ trợ random snapshot interval.
- Hỗ trợ simulated network latency.

## 16. Tính Năng Chưa Hoàn Thành / Hoàn Thành Một Phần

- Chưa có remote-player interpolation đúng nghĩa từ server snapshots.
- Chưa có snapshot buffer để render remote players giữa các server snapshots.
- Bot/remote players hiện vẫn chạy movement client-side bằng BotController, chưa phải server-authoritative movement.
- Bot positions chưa phải authoritative server state.
- Bot collect validation hiện vẫn phụ thuộc bot position do client gửi lên.
- Local player prediction/reconciliation có groundwork, nhưng enableClientPrediction đang tắt mặc định trong prefab.
- Smooth correction khi latency cao chưa polish.
- Chưa chuyển sang real network transport, mới có simulated transport.

## 17. Hướng Xử Lý Cho Phần Chưa Hoàn Thành

- Đưa bot/remote-player positions vào server state.
- Cho server simulation controller update toàn bộ player positions, không chỉ local player.
- Gửi tất cả player positions trong snapshots.
- Thêm snapshot buffering và interpolation cho non-local players.
- Cải thiện local player reconciliation bằng smooth correction và snap threshold.
- Chỉ giữ client-side prediction cho local keyboard player.
- Thay `SimulatedTransport` bằng transport thật phía sau `IGameTransport`.

Định hướng khi đưa bot lên server:

- Thêm `MapData` do server sở hữu để bot pathfinding dựa trên `MapData` thay vì đọc Unity scene.
- Chuyển bot movement simulation ra khỏi client `Update()` và đưa vào server state.
- Thêm bot state để lưu target egg hiện tại, waypoint/path đang đi, trạng thái bị block, kiểm tra khoảng cách collect, và retarget khi egg bị player khác lấy hoặc pathing lỗi.
- Server tự cập nhật vị trí bot và gửi vị trí đó trong snapshots.
- Bot không còn gửi collect request từ client. Server tự kiểm tra khoảng cách bot-to-egg và quyết định collect.
- Client chỉ còn render bot objects theo snapshots server gửi về.

## 18. Bug Còn Tồn Đọng

- Local player có thể đi xuyên qua bot. Hiện tại bot movement chạy ở client, còn local player movement được xử lý bởi server simulation controller. Vì server không sở hữu vị trí bot theo thời gian thực, server không thể dùng bot làm vật cản đáng tin cậy cho local player.
- Bot-to-bot blocking vẫn có thể trông như hoạt động đúng vì các bot đang được di chuyển bằng Unity `CharacterController` physics phía client. Đây là luồng khác với local player, vì local player đi theo vị trí đã được server xác nhận qua snapshot.

## 19. Cải Tiến Tương Lai

- Server-authoritative bot simulation.
- Remote-player interpolation từ snapshot buffer.
- Smooth local reconciliation khi latency cao.
- Real network transport implementation.
- Debug UI tốt hơn cho latency, queued messages, và snapshot timing.
- Thêm nhiều layout map và obstacle patterns.
