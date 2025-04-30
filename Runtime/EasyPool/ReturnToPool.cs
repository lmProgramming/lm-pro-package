﻿using UnityEngine;
using UnityEngine.Pool;

namespace LM.EasyPool
{
    public interface IReturnToPool<T> where T : Component
    {
        void Initialize(IObjectPool<T> pool);
        void OnConfigured();
        void ResetState();
    }
}