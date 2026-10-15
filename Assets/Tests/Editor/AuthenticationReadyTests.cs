using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace KitchenChaos.Tests
{
    public class AuthenticationReadyTests
    {
        private GameObject testObject;
        private Component lobby;
        private MethodInfo ensureServicesReady;

        [SetUp]
        public void SetUp()
        {
            // 测试程序集不能直接引用默认 Assembly-CSharp，通过反射调用真实入口。
            Type lobbyType = Type.GetType("GameLobby, Assembly-CSharp");
            Assert.That(lobbyType, Is.Not.Null, "找不到 GameLobby 运行时类型。");

            ensureServicesReady = lobbyType.GetMethod(
                "EnsureServicesReadyAsync", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(Func<Task>) }, null);
            Assert.That(ensureServicesReady, Is.Not.Null, "找不到共享认证入口。");

            // 不激活、不加载场景，避免触发 Awake 和真实 UGS 请求。
            testObject = new GameObject("AuthenticationReadyTests")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            testObject.SetActive(false);
            lobby = testObject.AddComponent(lobbyType);
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public async Task ConcurrentCallersShareOnePendingOperation()
        {
            var completion = new TaskCompletionSource<bool>();
            int calls = 0;
            Func<Task> operation = () =>
            {
                calls++;
                return completion.Task;
            };

            Task first = EnsureReady(operation);
            Task second = EnsureReady(operation);

            Assert.That(calls, Is.EqualTo(1), "并发入口不应重复启动认证。");
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.IsCompleted, Is.False);

            completion.SetResult(true);
            await Task.WhenAll(first, second);

            Assert.That(first.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(second.Status, Is.EqualTo(TaskStatus.RanToCompletion));
        }

        [Test]
        public async Task AuthenticationFailureReachesEveryWaitingCaller()
        {
            var completion = new TaskCompletionSource<bool>();
            var expected = new InvalidOperationException("Simulated authentication failure");
            Task first = EnsureReady(() => completion.Task);
            Task second = EnsureReady(() => completion.Task);

            completion.SetException(expected);

            Exception firstError = await CaptureFailureAsync(first);
            Exception secondError = await CaptureFailureAsync(second);

            Assert.That(firstError, Is.SameAs(expected), "第一个调用者必须收到认证异常。");
            Assert.That(secondError, Is.SameAs(expected), "其余等待者也必须收到认证异常。");
        }

        [Test]
        public async Task FailedOperationCanBeRetriedWithoutRecreatingLobby()
        {
            var firstCompletion = new TaskCompletionSource<bool>();
            var retryCompletion = new TaskCompletionSource<bool>();
            int calls = 0;
            Func<Task> operation = () =>
            {
                calls++;
                return calls == 1 ? firstCompletion.Task : retryCompletion.Task;
            };

            var expected = new InvalidOperationException("First attempt failed");
            Task failed = EnsureReady(operation);
            firstCompletion.SetException(expected);
            Assert.That(await CaptureFailureAsync(failed), Is.SameAs(expected));

            Task retry = EnsureReady(operation);
            Assert.That(calls, Is.EqualTo(2), "失败后应重新启动认证。");
            Assert.That(retry, Is.Not.SameAs(failed), "不能继续返回旧的失败任务。");
            Assert.That(retry.IsCompleted, Is.False);

            retryCompletion.SetResult(true);
            await retry;
            Assert.That(retry.Status, Is.EqualTo(TaskStatus.RanToCompletion));
        }

        private Task EnsureReady(Func<Task> operation)
        {
            return (Task)ensureServicesReady.Invoke(lobby, new object[] { operation });
        }

        private static async Task<Exception> CaptureFailureAsync(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
