using System;
using UnityEngine;

[Serializable]
public class FireController
{
    [SerializeField]
    [Range(0, 100)]
    public ulong increment = 1;
}
