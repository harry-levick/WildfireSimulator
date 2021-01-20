using System;
using UnityEngine;

namespace Assets.Scripts.Services
{
    public class UnityService : IUnityService
    {
        public bool GetKey(KeyCode key) => Input.GetKey(key);
        public bool GetMouseButton(int mouseButton) => Input.GetMouseButton(mouseButton);
        public bool GetMouseButtonDown(int mouseButton) => Input.GetMouseButtonDown(mouseButton);
        public bool GetMouseButtonUp(int mouseButton) => Input.GetMouseButtonUp(mouseButton);
    }
}
