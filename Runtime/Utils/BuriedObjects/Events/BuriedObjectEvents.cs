using UnityEngine;
using UnityEngine.Events;
using VoxelMarchingCubes.Utils.BuriedObjects.Core;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Events
{
    [System.Serializable]
    public class BuriedObjectEvents
    {
        [SerializeField]
        [Tooltip("When enabled, a debug message will be printed to the console whenever any buried-object event is invoked.")]
        private bool logInvocations = false;

        [System.Serializable]
        public class ExposureChangedEvent : UnityEvent<ExposureData> { }

        [System.Serializable]
        public class ObjectUnburiedEvent : UnityEvent<GameObject> { }

        [System.Serializable]
        public class ObjectPartiallyExposedEvent : UnityEvent<GameObject, float> { }

        public ExposureChangedEvent OnExposureChanged = new ExposureChangedEvent();
        public ObjectUnburiedEvent OnFullyExposed = new ObjectUnburiedEvent();
        public ObjectPartiallyExposedEvent OnPartiallyExposed = new ObjectPartiallyExposedEvent();
        public UnityEvent OnCompletelyBuried = new UnityEvent();

        // Wrapper invoke methods that optionally log to the console for better debugging/traceability
        public void InvokeExposureChanged(ExposureData data)
        {
            if (logInvocations)
            {
                Debug.Log($"[BuriedObjectEvents] OnExposureChanged -> {data}");
            }
            OnExposureChanged?.Invoke(data);
        }

        public void InvokeFullyExposed(GameObject go, ExposureData data)
        {
            if (logInvocations)
            {
                Debug.Log($"[BuriedObjectEvents] OnFullyExposed -> {go.name} | {data}");
            }
            OnFullyExposed?.Invoke(go);
        }

        public void InvokePartiallyExposed(GameObject go, float exposure, ExposureData data)
        {
            if (logInvocations)
            {
                Debug.Log($"[BuriedObjectEvents] OnPartiallyExposed -> {go.name} | Exposure: {exposure:P0} | {data}");
            }
            OnPartiallyExposed?.Invoke(go, exposure);
        }

        public void InvokeCompletelyBuried(ExposureData data)
        {
            if (logInvocations)
            {
                Debug.Log($"[BuriedObjectEvents] OnCompletelyBuried -> {data}");
            }
            OnCompletelyBuried?.Invoke();
        }
    }
}