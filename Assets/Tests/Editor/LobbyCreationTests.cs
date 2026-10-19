using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace KitchenChaos.Tests
{
    public class LobbyCreationTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private GameObject root;
        private NetworkManager manager;
        private Component lobby;
        private Component multiplayer;
        private Type lobbyType;
        private Type multiplayerType;
        private object previousManager;
        private object previousLobby;
        private object previousMultiplayer;
        private object previousLobbyService;
        private object previousRelayService;
        private object previousNoMessages;
        private FieldInfo noMessagesFlag;
        private FakeLobbyService lobbies;
        private FakeRelayService relay;
        private StartupFailureTransport transport;
        private readonly List<TaskCompletionSource<bool>> gates = new List<TaskCompletionSource<bool>>();

        [SetUp]
        public void SetUp()
        {
            gates.Clear();
            lobbyType = Type.GetType("GameLobby, Assembly-CSharp", true);
            multiplayerType = Type.GetType("GameMultiplayer, Assembly-CSharp", true);
            previousManager = NetworkManager.Singleton;
            previousLobby = lobbyType.GetProperty("Instance", Members).GetValue(null);
            previousMultiplayer = multiplayerType.GetProperty("Instance", Members).GetValue(null);
            previousLobbyService = typeof(LobbyService).GetProperty("service", Members).GetValue(null);
            previousRelayService = typeof(RelayService).GetField("m_Service", Members).GetValue(null);
            lobbies = new FakeLobbyService();
            relay = new FakeRelayService();
            SetSingleton(typeof(LobbyService), "Instance", lobbies);
            SetSingleton(typeof(RelayService), "Instance", relay);
            noMessagesFlag = typeof(NetworkManager).Assembly.GetType("Unity.Netcode.ILPPMessageProvider", true)
                .GetField("IntegrationTestNoMessages", Members);
            previousNoMessages = noMessagesFlag.GetValue(null);
            noMessagesFlag.SetValue(null, true);

            root = new GameObject("LobbyCreationTests") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            lobby = root.AddComponent(lobbyType);
            multiplayer = root.AddComponent(multiplayerType);
            Type listType = typeof(NetworkList<>).MakeGenericType(Type.GetType("PlayerData, Assembly-CSharp", true));
            SetField(multiplayer, "playerDataNetworkList", Activator.CreateInstance(listType));
            var managerObject = new GameObject("LobbyCreationTests.NetworkManager") { hideFlags = HideFlags.HideAndDontSave };
            managerObject.SetActive(false);
            manager = managerObject.AddComponent<NetworkManager>();
            transport = managerObject.AddComponent<StartupFailureTransport>();
            // 真实 UTP 只接收配置；实际启停交给不打开端口的测试 Transport。
            managerObject.AddComponent<UnityTransport>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, ConnectionApproval = true, EnableSceneManagement = false
            };
            SetSingleton(typeof(NetworkManager), "Singleton", manager);
            SetSingleton(lobbyType, "Instance", lobby);
            SetSingleton(multiplayerType, "Instance", multiplayer);
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                foreach (var gate in gates) gate.TrySetResult(true);
                if (lobby != null && (bool)GetField(lobby, "isCreatingLobby"))
                    await Until(() => !(bool)GetField(lobby, "isCreatingLobby"));
                if (multiplayer != null) Invoke(multiplayer, "UnregisterNetworkCallbacks");
                if (manager != null)
                {
                    SetProperty(manager, "IsListening", false);
                    object client = typeof(NetworkManager).GetProperty("LocalClient", Members).GetValue(manager);
                    SetProperty(client, "IsClient", false);
                    SetProperty(client, "IsServer", false);
                    Invoke(manager, "ShutdownInternal");
                }
                if (multiplayer != null) (GetField(multiplayer, "playerDataNetworkList") as IDisposable)?.Dispose();
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (manager != null) UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }
            finally
            {
                SetSingleton(typeof(LobbyService), "Instance", previousLobbyService);
                SetSingleton(typeof(RelayService), "Instance", previousRelayService);
                SetSingleton(typeof(NetworkManager), "Singleton", previousManager);
                if (lobbyType != null) SetSingleton(lobbyType, "Instance", previousLobby);
                if (multiplayerType != null) SetSingleton(multiplayerType, "Instance", previousMultiplayer);
                if (noMessagesFlag != null) noMessagesFlag.SetValue(null, previousNoMessages);
            }
        }

        [Test]
        public async Task AuthenticationFailureDoesNotCreateCloudResources()
        {
            var gate = ReadyGate();
            var expected = new InvalidOperationException("Simulated authentication failure.");
            Task task = (Task)Invoke(lobby, "CreateLobbyAsync", "Test", false);
            gate.SetException(expected);
            Assert.That(await Failure(task), Is.SameAs(expected));
            Assert.That(lobbies.CreateCalls, Is.Zero);
            Assert.That(lobbies.DeletedIds, Is.Empty);
        }

        [TestCase("Lobby")]
        [TestCase("Relay")]
        [TestCase("Code")]
        [TestCase("Update")]
        public async Task ServiceFailureRollsBackOnlyTheCreatedLobby(string stage)
        {
            var expected = new InvalidOperationException("Simulated " + stage + " failure.");
            if (stage == "Lobby") lobbies.CreateError = expected;
            if (stage == "Relay") relay.AllocateError = expected;
            if (stage == "Code") relay.CodeError = expected;
            if (stage == "Update") lobbies.UpdateError = expected;

            Exception observed = await Failure(StartCore());

            Assert.That(observed, Is.SameAs(expected), "不能把服务原始错误变成 null 或新的空引用异常。");
            Assert.That(GetField(lobby, "joinedLobby"), Is.Null);
            if (stage == "Lobby") Assert.That(lobbies.DeletedIds, Is.Empty);
            else Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1" }));
            Assert.That(transport.StartCalls, Is.Zero);
            if (stage == "Relay" || stage == "Lobby") Assert.That(relay.CodeCalls, Is.Zero);
            if (stage != "Update") Assert.That(lobbies.UpdateCalls, Is.Zero);
        }

        [Test]
        public async Task TransportConfigurationFailureDeletesLobbyBeforeStartingHost()
        {
            relay.Allocation = NewAllocation(false);
            Exception observed = await Failure(StartCore());
            Assert.That(observed, Is.InstanceOf<ArgumentException>());
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1" }));
            Assert.That(lobbies.UpdateCalls, Is.Zero);
            Assert.That(transport.StartCalls, Is.Zero);
        }

        [Test]
        public async Task HostStartFailureClosesItsSessionAndDeletesLobby()
        {
            LogAssert.Expect(LogType.Error, "[Netcode] Host is shutting down due to network transport start failure of StartupFailureTransport!");
            Exception observed = await Failure(StartCore());
            Assert.That(observed, Is.TypeOf<InvalidOperationException>());
            Assert.That(observed.Message, Is.EqualTo("Failed to start host."));
            Assert.That(transport.StartCalls, Is.EqualTo(1));
            Assert.That(manager.IsListening || manager.IsClient || manager.IsServer || manager.ShutdownInProgress, Is.False);
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1" }));
        }

        [Test]
        public async Task RejectedHostStartDoesNotShutDownExistingSession()
        {
            SetProperty(manager, "IsListening", true);
            Exception observed = await Failure(StartCore());
            Assert.That(observed, Is.TypeOf<InvalidOperationException>());
            Assert.That(manager.IsListening, Is.True);
            Assert.That(manager.ShutdownInProgress, Is.False);
            Assert.That(transport.StartCalls, Is.Zero);
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DeleteFailureDoesNotReplaceOriginalFailure(bool alreadyMissing)
        {
            var expected = new InvalidOperationException("Original allocation failure.");
            relay.AllocateError = expected;
            lobbies.DeleteError = alreadyMissing
                ? new LobbyServiceException(LobbyExceptionReason.LobbyNotFound, "Already gone.")
                : new InvalidOperationException("Delete unavailable.");
            if (!alreadyMissing) LogAssert.Expect(LogType.Error, "Failed to clean up lobby created-1: Delete unavailable.");
            Assert.That(await Failure(StartCore()), Is.SameAs(expected));
            Assert.That(GetField(lobby, "joinedLobby"), Is.Null);
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1" }));
        }

        [Test]
        public async Task RollbackPreservesAnUnrelatedLocalLobby()
        {
            var other = new Lobby(id: "other-room");
            SetField(lobby, "joinedLobby", other);
            await (Task)Invoke(lobby, "RollbackCreatedLobbyAsync", new Lobby(id: "owned-room"));
            Assert.That(GetField(lobby, "joinedLobby"), Is.SameAs(other));
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "owned-room" }));
        }

        [Test]
        public async Task DuplicateCreateIsIgnoredUntilRollbackFinishesAndThenCanRetry()
        {
            var expected = new InvalidOperationException("Simulated allocation failure.");
            relay.AllocateError = expected;
            var delete = Gate();
            lobbies.DeleteGate = delete.Task;
            int started = 0;
            int failed = 0;
            lobbyType.GetEvent("onCreateLobbyStarted").AddEventHandler(lobby, (EventHandler)((sender, args) => started++));
            lobbyType.GetEvent("onCreateLobbyFailed").AddEventHandler(lobby, (EventHandler)((sender, args) => failed++));
            var ready = ReadyGate();
            Invoke(lobby, "CreateLobby", "First", false);
            Invoke(lobby, "CreateLobby", "Duplicate", true);
            Assert.That(started, Is.EqualTo(1));
            Assert.That(lobbies.CreateCalls, Is.Zero);

            ready.SetResult(true);
            await Until(() => lobbies.DeletedIds.Count == 1);
            Assert.That((bool)GetField(lobby, "isCreatingLobby"), Is.True);
            Assert.That(failed, Is.Zero, "不能在回滚结束前报告本次流程结束。");
            Invoke(lobby, "CreateLobby", "During rollback", false);
            Assert.That(lobbies.CreateCalls, Is.EqualTo(1));
            LogAssert.Expect(LogType.Error, expected.Message);
            delete.SetResult(true);
            await Until(() => !(bool)GetField(lobby, "isCreatingLobby"));
            Assert.That(failed, Is.EqualTo(1));

            lobbies.DeleteGate = null;
            ready = ReadyGate();
            Invoke(lobby, "CreateLobby", "Retry", false);
            LogAssert.Expect(LogType.Error, expected.Message);
            ready.SetResult(true);
            await Until(() => !(bool)GetField(lobby, "isCreatingLobby"));
            Assert.That(lobbies.CreateCalls, Is.EqualTo(2));
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "created-1", "created-2" }));
            Assert.That(started, Is.EqualTo(2));
            Assert.That(failed, Is.EqualTo(2));
        }

        [Test]
        public void ExistingLobbyBlocksAnotherCreate()
        {
            SetField(lobby, "joinedLobby", new Lobby(id: "already-joined"));
            Invoke(lobby, "CreateLobby", "Ignored", false);
            Assert.That(lobbies.CreateCalls, Is.Zero);
            Assert.That((bool)GetField(lobby, "isCreatingLobby"), Is.False);
        }

        [Test]
        public async Task RollbackWaitsForActiveSessionToStopBeforeDeletingLobby()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            var owned = new Lobby(id: "owned-room");
            SetField(lobby, "joinedLobby", owned);

            Task rollback = (Task)Invoke(lobby, "RollbackCreationAsync", owned, multiplayer);

            Assert.That(rollback.IsCompleted, Is.False);
            Assert.That(manager.ShutdownInProgress, Is.True);
            Assert.That(lobbies.DeletedIds, Is.Empty, "网络还没关闭时，不应提前认为回滚结束。");
            Invoke(manager, "ShutdownInternal");
            await rollback;
            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "owned-room" }));
            Assert.That(manager.IsClient || manager.IsServer || manager.IsListening || manager.ShutdownInProgress, Is.False);
            Assert.That(GetField(lobby, "joinedLobby"), Is.Null);
        }

        [Test]
        public async Task ShutdownTimeoutStillAttemptsLobbyDeletion()
        {
            // 保持关闭状态不清空，验证停止超时不阻断另一项资源的清理。
            SetProperty(manager, "IsListening", true);
            var owned = new Lobby(id: "owned-room");
            SetField(lobby, "joinedLobby", owned);
            LogAssert.Expect(LogType.Error, "Failed to stop host during lobby rollback: Timed out waiting for the network session to shut down.");

            await (Task)Invoke(lobby, "RollbackCreationAsync", owned, multiplayer);

            Assert.That(lobbies.DeletedIds, Is.EqualTo(new[] { "owned-room" }));
            Assert.That(GetField(lobby, "joinedLobby"), Is.Null);
            Assert.That(manager.IsListening, Is.True, "超时后仍应如实保留未关闭状态。");
        }

        private Task StartCore()
        {
            var ready = ReadyGate();
            Task task = (Task)Invoke(lobby, "CreateLobbyAsync", "Test", false);
            ready.SetResult(true);
            return task;
        }

        private TaskCompletionSource<bool> Gate()
        {
            var gate = new TaskCompletionSource<bool>();
            gates.Add(gate);
            return gate;
        }

        private TaskCompletionSource<bool> ReadyGate()
        {
            var ready = Gate();
            SetField(lobby, "servicesReadyTask", ready.Task);
            return ready;
        }

        private static async Task<Exception> Failure(Task task)
        {
            try { await task; return null; }
            catch (Exception exception) { return exception; }
        }

        private static async Task Until(Func<bool> condition)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (!condition())
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) Assert.Fail("等待测试条件超时。");
                await Task.Yield();
            }
        }

        private static object GetField(object target, string name) => target.GetType().GetField(name, Members).GetValue(target);
        private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Members).SetValue(target, value);
        private static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name, Members).SetValue(target, value);
        private static void SetSingleton(Type type, string name, object value) => type.GetProperty(name, Members).SetValue(null, value);
        private static object Invoke(Component target, string method, params object[] args)
        {
            try { return target.GetType().GetMethod(method, Members).Invoke(target, args); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static Allocation NewAllocation(bool dtls = true) => new Allocation(Guid.NewGuid(),
            new List<RelayServerEndpoint> { new RelayServerEndpoint(dtls ? "dtls" : "udp", RelayServerEndpoint.NetworkOptions.Udp, false, dtls, "127.0.0.1", 7777) },
            new RelayServer("127.0.0.1", 7777), new byte[64], new byte[255], new byte[16], "test");

        // 未明确配置的服务方法一律抛异常，确保测试不会意外调用真实服务。
        private sealed class FakeRelayService : IRelayService
        {
            public Exception AllocateError;
            public Exception CodeError;
            public int CodeCalls;
            public Allocation Allocation = NewAllocation();
            public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null) =>
                AllocateError == null ? Task.FromResult(Allocation) : Task.FromException<Allocation>(AllocateError);
            public Task<string> GetJoinCodeAsync(Guid allocationId)
            {
                CodeCalls++;
                return CodeError == null ? Task.FromResult("test-code") : Task.FromException<string>(CodeError);
            }
            public Task<JoinAllocation> JoinAllocationAsync(string joinCode) => throw new NotSupportedException();
            public Task<List<Region>> ListRegionsAsync() => throw new NotSupportedException();
        }

        private sealed class FakeLobbyService : ILobbyService
        {
            public bool ConcurrencyControlEnabled { get; set; }
            public Exception CreateError;
            public Exception UpdateError;
            public Exception DeleteError;
            public Task DeleteGate;
            public int CreateCalls;
            public int UpdateCalls;
            public readonly List<string> DeletedIds = new List<string>();
            public Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, CreateLobbyOptions options = default)
            {
                CreateCalls++;
                return CreateError == null ? Task.FromResult(new Lobby(id: "created-" + CreateCalls)) : Task.FromException<Lobby>(CreateError);
            }
            public Task<Lobby> UpdateLobbyAsync(string lobbyId, UpdateLobbyOptions options)
            {
                UpdateCalls++;
                return UpdateError == null ? Task.FromResult(new Lobby(id: lobbyId)) : Task.FromException<Lobby>(UpdateError);
            }
            public Task DeleteLobbyAsync(string lobbyId)
            {
                DeletedIds.Add(lobbyId);
                return DeleteError != null ? Task.FromException(DeleteError) : DeleteGate ?? Task.CompletedTask;
            }
            public Task<Lobby> CreateOrJoinLobbyAsync(string id, string name, int max, CreateLobbyOptions options = default) => throw new NotSupportedException();
            public Task<ILobbyEvents> SubscribeToLobbyEventsAsync(string id, LobbyEventCallbacks callbacks) => throw new NotSupportedException();
            public Task<List<string>> GetJoinedLobbiesAsync() => throw new NotSupportedException();
            public Task<Lobby> GetLobbyAsync(string id) => throw new NotSupportedException();
            public Task<Lobby> GetLobbyAsync(string id, string version) => throw new NotSupportedException();
            public Task SendHeartbeatPingAsync(string id) => throw new NotSupportedException();
            public Task<Lobby> JoinLobbyByCodeAsync(string code, JoinLobbyByCodeOptions options = default) => throw new NotSupportedException();
            public Task<Lobby> JoinLobbyByIdAsync(string id, JoinLobbyByIdOptions options = default) => throw new NotSupportedException();
            public Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options = default) => throw new NotSupportedException();
            public Task<Lobby> QuickJoinLobbyAsync(QuickJoinLobbyOptions options = default) => throw new NotSupportedException();
            public Task RemovePlayerAsync(string id, string playerId) => throw new NotSupportedException();
            public Task<Lobby> UpdatePlayerAsync(string id, string playerId, UpdatePlayerOptions options) => throw new NotSupportedException();
            public Task<Lobby> ReconnectToLobbyAsync(string id) => throw new NotSupportedException();
            public Task<MigrationDataInfo> GetMigrationDataInfoAsync(string id) => throw new NotSupportedException();
            public Task<LobbyMigrationData> DownloadMigrationDataAsync(MigrationDataInfo data, LobbyDownloadMigrationDataOptions options) => throw new NotSupportedException();
            public Task<LobbyUploadMigrationDataResults> UploadMigrationDataAsync(MigrationDataInfo data, byte[] bytes, LobbyUploadMigrationDataOptions options) => throw new NotSupportedException();
        }
    }
}
