using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace KitchenChaos.Tests
{
    public class NetworkStartupTests
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        private GameObject root;
        private Component networkManager;
        private Component multiplayer;
        private Component lobby;
        private StartupFailureTransport transport;
        private Type networkManagerType;
        private Type multiplayerType;
        private Type lobbyType;
        private object previousNetworkManager;
        private object previousMultiplayer;
        private object previousLobby;
        private FieldInfo noMessagesFlag;
        private object previousNoMessages;

        [SetUp]
        public void SetUp()
        {
            networkManagerType = Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime", true);
            multiplayerType = Type.GetType("GameMultiplayer, Assembly-CSharp", true);
            lobbyType = Type.GetType("GameLobby, Assembly-CSharp", true);
            previousNetworkManager = GetStatic(networkManagerType, "Singleton");
            previousMultiplayer = GetStatic(multiplayerType, "Instance");
            previousLobby = GetStatic(lobbyType, "Instance");
            // NGO 自带的集成测试开关：本组只验证启停，不收发网络消息。
            // EditMode 不会自动执行 PlayMode 的消息注册初始化。
            noMessagesFlag = typeof(NetworkManager).Assembly.GetType("Unity.Netcode.ILPPMessageProvider", true)
                .GetField("IntegrationTestNoMessages", Members);
            previousNoMessages = noMessagesFlag.GetValue(null);
            noMessagesFlag.SetValue(null, true);

            root = new GameObject("NetworkStartupTests") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            // NGO 要求 NetworkManager 是根对象。对象不激活，避免进入用户的游戏流程。
            var managerObject = new GameObject("StartupTests.NetworkManager") { hideFlags = HideFlags.HideAndDontSave };
            managerObject.SetActive(false);
            networkManager = managerObject.AddComponent(networkManagerType);
            transport = managerObject.AddComponent<StartupFailureTransport>();
            ((NetworkManager)networkManager).NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = false
            };
            multiplayer = AddChild("GameMultiplayer", multiplayerType);
            Type playerListType = typeof(NetworkList<>).MakeGenericType(RuntimeType("PlayerData"));
            SetField(multiplayer, "playerDataNetworkList", Activator.CreateInstance(playerListType));
            lobby = AddChild("GameLobby", lobbyType);
            SetStatic(networkManagerType, "Singleton", networkManager);
            SetStatic(multiplayerType, "Instance", multiplayer);
            SetStatic(lobbyType, "Instance", lobby);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (networkManager != null)
                {
                    // 先退订业务回调，再同步释放测试自己的 NGO 状态。
                    if (multiplayer != null) Invoke(multiplayer, "UnregisterNetworkCallbacks");
                    SetListening(false);
                    object localClient = networkManagerType.GetProperty("LocalClient", Members).GetValue(networkManager);
                    SetProperty(localClient, "IsClient", false);
                    SetProperty(localClient, "IsServer", false);
                    Invoke(networkManager, "ShutdownInternal");
                }
                if (multiplayer != null)
                {
                    ((IDisposable)multiplayerType.GetField("playerDataNetworkList", Members).GetValue(multiplayer))?.Dispose();
                }
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
                if (networkManager != null) UnityEngine.Object.DestroyImmediate(networkManager.gameObject);
            }
            finally
            {
                // 单例替换只存在于测试期间，不接管用户场景中的对象。
                if (networkManagerType != null) SetStatic(networkManagerType, "Singleton", previousNetworkManager);
                if (multiplayerType != null) SetStatic(multiplayerType, "Instance", previousMultiplayer);
                if (lobbyType != null) SetStatic(lobbyType, "Instance", previousLobby);
                if (noMessagesFlag != null) noMessagesFlag.SetValue(null, previousNoMessages);
            }
        }

        [Test]
        public void HostRejectsRepeatedStartBeforeCallerContinues()
        {
            SetListening(true);
            bool continued = false;
            InvalidOperationException observed = null;

            try
            {
                Invoke(multiplayer, "StartHost");
                continued = true;
            }
            catch (InvalidOperationException exception)
            {
                observed = exception;
            }

            Assert.That(observed, Is.Not.Null, "启动失败必须抛给调用者。");
            Assert.That(observed.Message, Is.EqualTo("Network session is already running or shutting down."));
            Assert.That(continued, Is.False, "失败后调用者不能继续执行下一步。");
        }

        [Test]
        public void ClientRejectsRepeatedStartWithoutJoinNotifications()
        {
            SetListening(true);
            Component connecting = AddChild("ConnectingUI", RuntimeType("ConnectingUI"));
            connecting.gameObject.SetActive(false);
            Component message = CreateMessageUI(out _);
            var notifications = new List<string>();
            bool showedConnecting = false;

            EventHandler trying = (sender, args) =>
            {
                notifications.Add("trying");
                Invoke(connecting, "GameMultiplayer_OnTryingToJoinGame", sender, args);
                showedConnecting = connecting.gameObject.activeSelf;
            };
            EventHandler failed = (sender, args) =>
            {
                notifications.Add("failed");
                Invoke(message, "GameMultiplayer_OnFailedToJoinGame", sender, args);
                Invoke(connecting, "GameMultiplayer_OnFailedToJoinGame", sender, args);
            };
            SetField(multiplayer, "OnTryingToJoinGame", trying);
            SetField(multiplayer, "OnFailedToJoinGame", failed);

            InvalidOperationException observed = Assert.Throws<InvalidOperationException>(
                () => Invoke(multiplayer, "StartClient"));

            Assert.That(observed.Message, Is.EqualTo("Network session is already running or shutting down."));
            Assert.That(notifications, Is.Empty, "拒绝重复启动不能发送新的连接或失败通知。");
            Assert.That(showedConnecting, Is.False);
            Assert.That(connecting.gameObject.activeSelf, Is.False);
            Assert.That(message.gameObject.activeSelf, Is.False);
        }

        [TestCase("StartHost", false)]
        [TestCase("StartClient", false)]
        [TestCase("StartHost", true)]
        [TestCase("StartClient", true)]
        public void FailedStartRemovesOwnCallbacksAndAllowsAnotherAttempt(string method, bool initializationThrows)
        {
            transport.FailInitialization = initializationThrows;
            var notifications = new List<string>();
            SetField(multiplayer, "OnTryingToJoinGame", (EventHandler)((sender, args) => notifications.Add("trying")));
            SetField(multiplayer, "OnFailedToJoinGame", (EventHandler)((sender, args) => notifications.Add("failed")));
            Action<ulong> unrelated = id => { };
            ((NetworkManager)networkManager).OnClientConnectedCallback += unrelated;
            ((NetworkManager)networkManager).OnClientDisconnectCallback += unrelated;
            int boundConnected = 0;
            int boundDisconnected = 0;
            transport.DuringStart = () =>
            {
                boundConnected = OwnCallbackCount("OnClientConnectedCallback");
                boundDisconnected = OwnCallbackCount("OnClientDisconnectCallback");
            };

            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (initializationThrows)
                    LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Simulated transport initialization failure\\."));
                else
                    LogAssert.Expect(LogType.Error, $"[Netcode] {(method == "StartHost" ? "Host" : "Client")} is shutting down due to network transport start failure of StartupFailureTransport!");

                InvalidOperationException observed = Assert.Throws<InvalidOperationException>(() => Invoke(multiplayer, method));
                Assert.That(observed.Message, Is.EqualTo(method == "StartHost" ? "Failed to start host." : "Failed to start client."));
                Assert.That(OwnCallbackCount("OnClientConnectedCallback"), Is.Zero);
                Assert.That(OwnCallbackCount("OnClientDisconnectCallback"), Is.Zero);
                Assert.That(((NetworkManager)networkManager).ConnectionApprovalCallback, Is.Null);
                Assert.That(Callbacks("OnClientConnectedCallback"), Does.Contain(unrelated));
                Assert.That(Callbacks("OnClientDisconnectCallback"), Does.Contain(unrelated));
                Assert.That(multiplayerType.GetField("callbackNetworkManager", Members).GetValue(multiplayer), Is.Null);
            }
            if (!initializationThrows)
            {
                Assert.That(transport.StartCalls, Is.EqualTo(2));
                Assert.That(boundConnected, Is.EqualTo(1));
                Assert.That(boundDisconnected, Is.EqualTo(1));
            }
            if (method == "StartClient")
                Assert.That(notifications, Is.EqualTo(new[] { "trying", "failed", "trying", "failed" }));
            else
                Assert.That(notifications, Is.Empty);
        }

        [TestCase("StartHost", "IsListening")]
        [TestCase("StartClient", "IsListening")]
        [TestCase("StartHost", "IsClient")]
        [TestCase("StartClient", "IsClient")]
        [TestCase("StartHost", "IsServer")]
        [TestCase("StartClient", "IsServer")]
        [TestCase("StartHost", "ShutdownInProgress")]
        [TestCase("StartClient", "ShutdownInProgress")]
        public void BusySessionKeepsExistingCallbacks(string method, string busyState)
        {
            // 用无真实连接的 Transport 启动本地 Client，以获得真实业务订阅。
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            SetListening(false);
            object localClient = networkManagerType.GetProperty("LocalClient", Members).GetValue(networkManager);
            SetProperty(localClient, "IsClient", false);
            if (busyState == "IsListening") SetListening(true);
            else if (busyState == "ShutdownInProgress") SetField(networkManager, "m_ShuttingDown", true);
            else SetProperty(localClient, busyState, true);
            Delegate[] beforeConnected = Callbacks("OnClientConnectedCallback");
            Delegate[] beforeDisconnected = Callbacks("OnClientDisconnectCallback");
            int notifications = 0;
            SetField(multiplayer, "OnTryingToJoinGame", (EventHandler)((sender, args) => notifications++));
            SetField(multiplayer, "OnFailedToJoinGame", (EventHandler)((sender, args) => notifications++));

            Assert.Throws<InvalidOperationException>(() => Invoke(multiplayer, method));

            Assert.That(Callbacks("OnClientConnectedCallback"), Is.EqualTo(beforeConnected));
            Assert.That(Callbacks("OnClientDisconnectCallback"), Is.EqualTo(beforeDisconnected));
            Assert.That(OwnCallbackCount("OnClientConnectedCallback"), Is.EqualTo(1));
            Assert.That(transport.StartCalls, Is.EqualTo(1));
            Assert.That(notifications, Is.Zero);
            SetProperty(localClient, "IsClient", false);
            SetProperty(localClient, "IsServer", false);
        }

        [TestCase("StartHost")]
        [TestCase("StartClient")]
        public void MissingManagerFailsBeforeJoinNotification(string method)
        {
            SetStatic(networkManagerType, "Singleton", null);
            int notifications = 0;
            SetField(multiplayer, "OnTryingToJoinGame", (EventHandler)((sender, args) => notifications++));
            var observed = Assert.Throws<InvalidOperationException>(() => Invoke(multiplayer, method));
            Assert.That(observed.Message, Is.EqualTo("NetworkManager is not available."));
            Assert.That(notifications, Is.Zero);
            SetStatic(networkManagerType, "Singleton", networkManager);
        }

        [Test]
        public void DestroyCleanupRemovesOnlyOwnCallbacksAndCanRepeat()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            Action<ulong> unrelated = id => { };
            ((NetworkManager)networkManager).OnClientConnectedCallback += unrelated;
            ((NetworkManager)networkManager).OnClientDisconnectCallback += unrelated;

            // 不激活测试对象，显式执行真实销毁回调。
            Invoke(multiplayer, "OnDestroy");
            Invoke(multiplayer, "UnregisterNetworkCallbacks");

            Assert.That(OwnCallbackCount("OnClientConnectedCallback"), Is.Zero);
            Assert.That(OwnCallbackCount("OnClientDisconnectCallback"), Is.Zero);
            Assert.That(Callbacks("OnClientConnectedCallback"), Does.Contain(unrelated));
            Assert.That(Callbacks("OnClientDisconnectCallback"), Does.Contain(unrelated));
        }

        [Test]
        public void ActiveShutdownRemovesOwnCallbacksWithoutReportingJoinFailure()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            var manager = (NetworkManager)networkManager;
            int failures = 0;
            int unrelatedDisconnects = 0;
            SetField(multiplayer, "OnFailedToJoinGame", (EventHandler)((sender, args) => failures++));
            Action<ulong> unrelatedConnected = id => { };
            Action<ulong> unrelatedDisconnected = id => unrelatedDisconnects++;
            Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> unrelatedApproval =
                (request, response) => { };
            manager.OnClientConnectedCallback += unrelatedConnected;
            manager.OnClientDisconnectCallback += unrelatedDisconnected;
            manager.ConnectionApprovalCallback = unrelatedApproval;

            Invoke(multiplayer, "ShutdownNetwork");

            Assert.That(manager.ShutdownInProgress, Is.True, "这里只请求停止，不能假定 NGO 已关闭。");
            Assert.That(OwnCallbackCount("OnClientConnectedCallback"), Is.Zero);
            Assert.That(OwnCallbackCount("OnClientDisconnectCallback"), Is.Zero);
            Assert.That(Callbacks("OnClientConnectedCallback"), Does.Contain(unrelatedConnected));
            Assert.That(Callbacks("OnClientDisconnectCallback"), Does.Contain(unrelatedDisconnected));
            Assert.That(manager.ConnectionApprovalCallback, Is.EqualTo(unrelatedApproval));
            Assert.That(multiplayerType.GetField("callbackNetworkManager", Members).GetValue(multiplayer), Is.Null);

            // 模拟停止请求之后到达的真实 NGO 断线通知。
            object connections = networkManagerType.GetField("ConnectionManager", Members).GetValue(networkManager);
            connections.GetType().GetMethod("InvokeOnClientDisconnectCallback", Members)
                .Invoke(connections, new object[] { 0UL });
            Assert.That(failures, Is.Zero, "主动停止不能被自己的回调当成加入失败。");
            Assert.That(unrelatedDisconnects, Is.EqualTo(1), "其他系统仍应收到断线通知。");
            Assert.DoesNotThrow(() => Invoke(multiplayer, "ShutdownNetwork"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedShutdownIsSafeWithoutActiveSession(bool missingManager)
        {
            if (missingManager) SetStatic(networkManagerType, "Singleton", null);

            Assert.DoesNotThrow(() => Invoke(multiplayer, "ShutdownNetwork"));
            Assert.DoesNotThrow(() => Invoke(multiplayer, "ShutdownNetwork"));

            Assert.That(transport.StartCalls, Is.Zero);
            Assert.That(((NetworkManager)networkManager).ShutdownInProgress, Is.False);
        }

        [Test]
        public void RestartIsBlockedUntilShutdownFinishesThenRegistersOnce()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            Invoke(multiplayer, "ShutdownNetwork");

            Assert.Throws<InvalidOperationException>(() => Invoke(multiplayer, "StartHost"));
            Assert.Throws<InvalidOperationException>(() => Invoke(multiplayer, "StartClient"));
            Assert.That(transport.StartCalls, Is.EqualTo(1));

            // EditMode 中显式完成 NGO 清理，不依赖 PlayerLoop 的后续帧。
            Invoke(networkManager, "ShutdownInternal");
            Assert.That(((NetworkManager)networkManager).ShutdownInProgress, Is.False);
            Invoke(multiplayer, "StartClient");

            Assert.That(transport.StartCalls, Is.EqualTo(2));
            Assert.That(OwnCallbackCount("OnClientConnectedCallback"), Is.EqualTo(1));
            Assert.That(OwnCallbackCount("OnClientDisconnectCallback"), Is.EqualTo(1));
        }

        [Test]
        public async Task AsyncShutdownWaitsForCompletionAndSupportsRepeatedCalls()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            Task first = InvokeTask(multiplayer, "ShutdownNetworkAsync");
            Task second = InvokeTask(multiplayer, "ShutdownNetworkAsync");
            Assert.That(first.IsCompleted, Is.False);
            Assert.That(second.IsCompleted, Is.False);
            Assert.That(((NetworkManager)networkManager).ShutdownInProgress, Is.True);

            Invoke(networkManager, "ShutdownInternal");
            await Task.WhenAll(first, second);

            Assert.That(((NetworkManager)networkManager).IsClient, Is.False);
            Assert.That(((NetworkManager)networkManager).ShutdownInProgress, Is.False);
            Assert.That(OwnCallbackCount("OnClientDisconnectCallback"), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task AsyncShutdownCompletesWithoutAnActiveSession(bool missingManager)
        {
            if (missingManager) SetStatic(networkManagerType, "Singleton", null);
            Task task = InvokeTask(multiplayer, "ShutdownNetworkAsync");
            Assert.That(task.IsCompleted, Is.True);
            await task;
        }

        [Test]
        public async Task AsyncShutdownTimesOutWhilePausedWithoutPretendingItStopped()
        {
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                // 人为保持一个未清空的状态，不启动实际连接或 NGO 更新循环。
                SetListening(true);
                Exception observed = null;
                try { await InvokeTask(multiplayer, "ShutdownNetworkAsync"); }
                catch (Exception exception) { observed = exception; }
                Assert.That(observed, Is.TypeOf<TimeoutException>());
                Assert.That(((NetworkManager)networkManager).IsListening, Is.True);
            }
            finally { Time.timeScale = previousTimeScale; }
        }

        [Test]
        public void LoaderReturnsSceneRequestRejectionWithoutLoadingAScene()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            Type loaderType = RuntimeType("Loader");
            object scene = Enum.Parse(loaderType.GetNestedType("Scene"), "CharacterSelectScene");
            LogAssert.Expect(LogType.Warning, "LoadScene was called, but EnableSceneManagement was not enabled! Enable EnableSceneManagement prior to starting a client, host, or server prior to using NetworkSceneManager!");

            object status = loaderType.GetMethod("LoadNetwork", Members).Invoke(null, new[] { scene });

            Assert.That(status, Is.EqualTo(SceneEventProgressStatus.SceneManagementNotEnabled));
        }

        private static Task InvokeTask(Component target, string method) =>
            (Task)target.GetType().GetMethod(method, Members).Invoke(target, null);

        [Test]
        public void ServerShutdownClearsPreviousPlayersBeforeRetry()
        {
            PopulatePreviousRoster();
            Assert.That(RosterCount(), Is.EqualTo(2));

            Invoke(multiplayer, "ShutdownNetwork");

            Assert.That(RosterCount(), Is.Zero, "停止 Host 后不能保留上一局的玩家记录。");
            Invoke(networkManager, "ShutdownInternal");
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            object localClient = networkManagerType.GetProperty("LocalClient", Members).GetValue(networkManager);
            SetProperty(localClient, "IsServer", true);
            // 模拟重试时 Host 的连接通知，调用真实的名单处理方法。
            Invoke(multiplayer, "NetworkManager_OnClientConnectedCallback", 0UL);
            Assert.That(RosterCount(), Is.EqualTo(1));
            object player = multiplayerType.GetMethod("GetPlayerDataFromIndex", Members).Invoke(multiplayer, new object[] { 0 });
            Assert.That(player.GetType().GetField("colorID").GetValue(player), Is.EqualTo(0));
        }

        [Test]
        public void ClientShutdownDoesNotClearServerOwnedPlayers()
        {
            PopulatePreviousRoster();
            object localClient = networkManagerType.GetProperty("LocalClient", Members).GetValue(networkManager);
            SetProperty(localClient, "IsServer", false);

            Invoke(multiplayer, "ShutdownNetwork");

            Assert.That(RosterCount(), Is.EqualTo(2), "Client 不能直接清理服务端写权限的名单。");
        }

        private void PopulatePreviousRoster()
        {
            transport.StartResult = true;
            Invoke(multiplayer, "StartClient");
            object localClient = networkManagerType.GetProperty("LocalClient", Members).GetValue(networkManager);
            SetProperty(localClient, "IsServer", true);
            SetField(multiplayer, "playerColorList", new List<Color> { Color.red, Color.blue });
            // 本组只模拟服务端角色和连接通知，不创建真实 Host 或网络玩家物体。
            Invoke(multiplayer, "NetworkManager_OnClientConnectedCallback", 0UL);
            Invoke(multiplayer, "NetworkManager_OnClientConnectedCallback", 7UL);
        }

        private int RosterCount()
        {
            object roster = multiplayerType.GetField("playerDataNetworkList", Members).GetValue(multiplayer);
            return (int)roster.GetType().GetProperty("Count").GetValue(roster);
        }

        private Delegate[] Callbacks(string name)
        {
            object connections = networkManagerType.GetField("ConnectionManager", Members).GetValue(networkManager);
            return ((Delegate)connections.GetType().GetField(name, Members).GetValue(connections))?.GetInvocationList()
                ?? Array.Empty<Delegate>();
        }

        private int OwnCallbackCount(string name)
        {
            int count = 0;
            foreach (Delegate callback in Callbacks(name))
                if (ReferenceEquals(callback.Target, multiplayer)) count++;
            return count;
        }

        private static void SetProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, Members).SetValue(target, value);

        [TestCase(null, "Failed to connect.")]
        [TestCase("", "Failed to connect.")]
        [TestCase(" \t ", "Failed to connect.")]
        [TestCase("Server is full.", "Server is full.")]
        public void FailureMessageUsesFallbackOnlyForMissingReason(string reason, string expected)
        {
            object connectionManager = networkManagerType.GetField("ConnectionManager", Members)
                .GetValue(networkManager);
            SetField(connectionManager, "m_DisconnectReason", reason);
            Component message = CreateMessageUI(out Component label);

            Invoke(message, "GameMultiplayer_OnFailedToJoinGame", multiplayer, EventArgs.Empty);

            Assert.That(message.gameObject.activeSelf, Is.True);
            Assert.That(GetText(label), Is.EqualTo(expected));
        }

        private Component CreateMessageUI(out Component label)
        {
            Component message = AddChild("LobbyMessageUI", RuntimeType("LobbyMessageUI"));
            Component button = AddChild("BackButton", Type.GetType("UnityEngine.UI.Button, UnityEngine.UI", true));
            label = AddChild("MessageText", Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
            SetField(message, "backButton", button);
            SetField(message, "messageText", label);
            message.gameObject.SetActive(false);
            return message;
        }

        private Component AddChild(string name, Type componentType)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child.AddComponent(componentType);
        }

        private void SetListening(bool value)
        {
            networkManagerType.GetProperty("IsListening", Members).SetValue(networkManager, value);
        }

        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static object GetStatic(Type type, string name) => type.GetProperty(name, Members).GetValue(null);
        private static void SetStatic(Type type, string name, object value) => type.GetProperty(name, Members).SetValue(null, value);
        private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Members).SetValue(target, value);
        private static string GetText(Component label) => (string)label.GetType().GetProperty("text").GetValue(label);

        private static void Invoke(Component target, string method, params object[] args)
        {
            try
            {
                target.GetType().GetMethod(method, Members).Invoke(target, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
