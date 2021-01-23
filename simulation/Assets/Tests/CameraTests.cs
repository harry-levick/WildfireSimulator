using Assets.Scripts.Player;
using System.Collections;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Services;

namespace Assets.Tests
{
    public class CameraTests
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PressKeyWMovesCameraForwardTest()
        {
            var camera = new GameObject().AddComponent<Player>();
            var unityService = Substitute.For<IUnityService>();
            unityService.GetKey(KeyCode.W).Returns(true);

            camera.Settings.Speed = 1f;
            camera.UnityService = unityService;

            yield return null;

            Assert.AreEqual(Vector3.forward, camera.transform.position);
        }
    }
}
