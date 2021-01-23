using UnityEngine;

namespace Player.Services
{
    public interface IUnityService
    {
        bool GetMouseButtonDown(int mouseButton);
        bool GetMouseButtonUp(int mouseButton);
        bool GetKey(KeyCode key);
        bool GetMouseButton(int mouseButton);
    }
}
