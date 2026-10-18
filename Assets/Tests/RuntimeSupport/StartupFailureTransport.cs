using System;
using Unity.Netcode;

namespace KitchenChaos.Tests
{
    // 仅供测试程序集引用，不打开端口、不连接 UGS。
    public class StartupFailureTransport : NetworkTransport
    {
        public bool FailInitialization;
        public bool StartResult;
        public int StartCalls;
        public Action DuringStart;

        public override ulong ServerClientId => 0;

        public override void Initialize(NetworkManager networkManager = null)
        {
            if (FailInitialization)
                throw new InvalidOperationException("Simulated transport initialization failure.");
        }

        public override bool StartClient() => StartTransport();
        public override bool StartServer() => StartTransport();

        private bool StartTransport()
        {
            StartCalls++;
            DuringStart?.Invoke();
            return StartResult;
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = default;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery) { }
        public override void DisconnectRemoteClient(ulong clientId) { }
        public override void DisconnectLocalClient() { }
        public override ulong GetCurrentRtt(ulong clientId) => 0;
        public override void Shutdown() { }
    }
}
