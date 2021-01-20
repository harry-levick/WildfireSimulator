using UnityEngine;

namespace Assets.Scripts.Services
{
    public interface IUnityService
    {
        public bool GetMouseButtonDown(int mouseButton);
        public bool GetMouseButtonUp(int mouseButton);
        public bool GetKey(KeyCode key);
        public bool GetMouseButton(int mouseButton);
    }
}
