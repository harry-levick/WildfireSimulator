using System;
using UnityEngine;

namespace Services
{
    public class UnityService : IUnityService
    {
        public bool GetKey(KeyCode key) => Input.GetKey(key);
        public bool GetMouseButton(int mouseButton) => Input.GetMouseButton(mouseButton);
        public float GetMouseScrollDelta()
        {
            var scroll = Input.mouseScrollDelta;
            return Mathf.Abs(scroll.y) > Mathf.Abs(scroll.x) ? scroll.y : scroll.x;
        } 
        public bool GetMouseButtonDown(int mouseButton) => Input.GetMouseButtonDown(mouseButton);
        public bool GetMouseButtonUp(int mouseButton) => Input.GetMouseButtonUp(mouseButton);
    }
}
