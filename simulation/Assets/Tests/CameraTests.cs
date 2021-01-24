using Player;
using System.Collections;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Services;

namespace Tests
{
    public class CameraTests
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PressKeyWMovesCameraForwardTest()
        {
            var camera = new GameObject().AddComponent<PlayerController>();
            var unityService = Substitute.For<IUnityService>();
            unityService.GetKey(KeyCode.W).Returns(true);

            camera.settings.Speed = 1f;
            camera.UnityService = unityService;

            yield return null;

            Assert.AreEqual(Vector3.forward, camera.transform.position);
        }
    }
}
