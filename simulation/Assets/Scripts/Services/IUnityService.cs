using UnityEngine;

namespace Assets.Scripts.Services
{
    public interface IUnityService
    {
        bool GetMouseButtonDown(int mouseButton);
        bool GetMouseButtonUp(int mouseButton);
        bool GetKey(KeyCode key);
        bool GetMouseButton(int mouseButton);
    }
}
