using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CatMe.Tests
{
    public sealed class EditModeAuditTests
    {
        static Assembly App => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
        static Type AppType(string name) => App.GetType(name, true);

        [Test]
        public void PetRoster_MergesServerPetOnlyOnce()
        {
            Type roster = AppType("CatMe.Pets.PetRoster");
            string root = Path.Combine(Path.GetTempPath(), "worntag-roster-" + Guid.NewGuid().ToString("N"));
            try
            {
                roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { root });
                MethodInfo add = roster.GetMethod("AddServerPet");
                add.Invoke(null, new object[] { "server-pet-1", "Mochi", "cat" });
                add.Invoke(null, new object[] { "server-pet-1", "Changed", "dog" });
                var pets = ((IEnumerable)roster.GetProperty("Pets").GetValue(null)).Cast<object>().ToArray();
                Assert.That(pets, Has.Length.EqualTo(1));
                Assert.That(pets[0].GetType().GetField("serverId").GetValue(pets[0]), Is.EqualTo("server-pet-1"));
            }
            finally
            {
                roster.GetMethod("SetStorageRootForTests").Invoke(null, new object[] { null });
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestCase(0, null, "No internet connection. Try again when you're back online.")]
        [TestCase(429, null, "Too many tries. Wait a minute.")]
        [TestCase(503, null, "The WornTag server is having trouble. Try again shortly.")]
        [TestCase(429, "{\"error\":{\"code\":\"DAILY_CAP_REACHED\",\"message\":\"server text\"}}", "We are making a lot of pets today. Try again tomorrow.")]
        public void MobileApi_MapsErrorsByCodeBeforeStatus(long status, string body, string expected)
        {
            MethodInfo map = AppType("CatMe.Onboarding.MobileApi").GetMethod("ErrorMessage", new[] { typeof(long), typeof(string) });
            Assert.That(map.Invoke(null, new object[] { status, body }), Is.EqualTo(expected));
        }

        [Test]
        public void CompanionScaleStandards_UsesTargetOverMeasuredHeight()
        {
            Type standards = AppType("CatMe.Garden.CompanionScaleStandards");
            MethodInfo scale = standards.GetMethod("ScaleForMeasuredHeight", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That((float)scale.Invoke(null, new object[] { 2f, 1f }), Is.EqualTo(.5f));
            Assert.Throws<TargetInvocationException>(() => scale.Invoke(null, new object[] { 0f, 1f }));
        }

        [Test]
        public async Task CreationClient_ResumePollsAndDownloadsWithoutPostingAnotherPet()
        {
            Type apiType = AppType("CatMe.Onboarding.IMobileApi");
            MethodInfo createProxy = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == "Create" && m.IsGenericMethodDefinition).MakeGenericMethod(apiType, typeof(FakeMobileApi));
            object api = createProxy.Invoke(null, null);
            var fake = (FakeMobileApi)api;
            Type clientType = AppType("CatMe.Onboarding.CreationClient");
            object client = Activator.CreateInstance(clientType, api, new Func<int, Task>(_ => Task.CompletedTask));
            string destination = Path.Combine(Path.GetTempPath(), "worntag-model-" + Guid.NewGuid().ToString("N") + ".glb");
            Task task = (Task)clientType.GetMethod("ResumeAsync").Invoke(client, new object[] { "pet-1", destination, null, null });
            await task;
            object result = task.GetType().GetProperty("Result").GetValue(task);
            Assert.That((bool)result.GetType().GetField("ok").GetValue(result), Is.True);
            Assert.That(fake.CreatePetCalls, Is.Zero);
            Assert.That(fake.GetPetCalls, Is.EqualTo(2));
            Assert.That(fake.DownloadCalls, Is.EqualTo(1));
        }
    }

    public class FakeMobileApi : DispatchProxy
    {
        public int CreatePetCalls, GetPetCalls, DownloadCalls;

        protected override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == "CreatePetAsync") CreatePetCalls++;
            if (method.Name == "GetPetAsync")
            {
                GetPetCalls++;
                Type statusType = method.ReturnType.GetGenericArguments()[0];
                object status = Activator.CreateInstance(statusType);
                statusType.GetField("id").SetValue(status, "pet-1");
                statusType.GetField("status").SetValue(status, GetPetCalls == 1 ? "generating" : "ready");
                statusType.GetField("stage").SetValue(status, GetPetCalls == 1 ? "generating" : "ready");
                statusType.GetField("modelUrl").SetValue(status, "https://example.invalid/model.glb");
                return Completed(method.ReturnType, status);
            }
            if (method.Name == "GetOwnerAsync") return Completed(method.ReturnType, null);
            if (method.Name == "DownloadFileAsync") { DownloadCalls++; return Task.CompletedTask; }
            if (method.ReturnType == typeof(Task)) return Task.CompletedTask;
            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                return Completed(method.ReturnType, method.ReturnType.GetGenericArguments()[0] == typeof(string) ? "unused" : null);
            return null;
        }

        static object Completed(Type taskType, object value)
        {
            Type valueType = taskType.GetGenericArguments()[0];
            return typeof(Task).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "FromResult" && m.IsGenericMethodDefinition)
                .MakeGenericMethod(valueType).Invoke(null, new[] { value });
        }
    }
}
