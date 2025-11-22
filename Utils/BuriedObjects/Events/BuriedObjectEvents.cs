using UnityEngine;
using UnityEngine.Events;
using VoxelMarchingCubes.Utils.BuriedObjects.Core;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Events
{
    [System.Serializable]
    public class BuriedObjectEvents
    {
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
    }
}