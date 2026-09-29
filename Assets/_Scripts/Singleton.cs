using UnityEngine;

namespace _Scripts
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this as T)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this as T) Instance = null;
        }
    }

    public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            if (Instance == this as T)
                DontDestroyOnLoad(gameObject);
        }
    }
}