using System;
using UnityEngine;
using VoxelMarchingCubes.Runtime;
using VoxelMarchingCubes.Utils.BuriedObjects.Detection;
using VoxelMarchingCubes.Utils.BuriedObjects.Events;
using VoxelMarchingCubes.Utils.BuriedObjects.Managers;

namespace VoxelMarchingCubes.Utils.BuriedObjects.Core
{
    [RequireComponent(typeof(Collider))]
    public class BuriedObject : MonoBehaviour, IBuriable
    {
        [Header("Detection Settings")]
        [SerializeField] private bool autoDetectBounds = true;
        [SerializeField] private Bounds customBounds;
        [SerializeField] [Range(2, 10)] private int samplesPerAxis = 3;
        [SerializeField] private float densityThreshold = 0.5f;
        [SerializeField] private float sampleExpansion = 0.1f;

        [Header("Exposure Thresholds")]
        [SerializeField] [Range(0f, 1f)] private float fullyExposedThreshold = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float partiallyExposedThreshold = 0.2f;

        [Header("Events")]
        [SerializeField] private BuriedObjectEvents events = new BuriedObjectEvents();

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [Tooltip("Enable runtime debug logging for exposure state changes and diagnostics.")]
        [SerializeField] private bool enableLogging = false;
        [SerializeField] private Color buriedColor = Color.red;
        [SerializeField] private Color partialColor = Color.yellow;
        [SerializeField] private Color exposedColor = Color.green;
            
        private ExposureData _currentExposure = new ExposureData(0f,0,0,Vector3.zero, new Bounds());
        private ExposureState _previousState = ExposureState.Buried;
        private IExposureDetector _exposureDetector;
        private bool _hasBeenFullyExposed;
            
        public float Exposure => _currentExposure.Exposure;
        public Vector3 Position => transform.position;
        public ExposureData CurrentExposure => _currentExposure;
        public bool HasBeenFullyExposed => _hasBeenFullyExposed;
        public BuriedObjectEvents Events => events;
            
        private enum ExposureState
        {
            Buried,
            PartiallyExposed,
            FullyExposed
        }
        
        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDetector();
            
            if (autoDetectBounds)
            {
                customBounds = CalculateBounds();
            }
        }

        private void Start()
        {
            BuriedObjectManager.Instance?.RegisterObject(this);
        }

        private void OnDestroy()
        {
            if (BuriedObjectManager.HasInstance)
            {
                BuriedObjectManager.Instance?.UnregisterObject(this);
            }
        }
        
        #endregion

        #region Initialization
        
        private void InitializeDetector()
        {
            _exposureDetector = new VoxelDensityDetector(densityThreshold, samplesPerAxis, sampleExpansion);
        }
        
        private Bounds CalculateBounds()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds;
            }

            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                return rend.bounds;
            }
            
            return new Bounds(transform.position, Vector3.one);
        }
        
        #endregion
        
        #region IBuriable Implementation
        public void UpdateExposure(VoxelTerrain terrain)
        {
            if (terrain == null || !isActiveAndEnabled)
            {
                Debug.LogWarning($"Cannot update exposure for {name}: missing terrain or not active");
                return;
            }
            
            // Avoid evaluating exposure before terrain has finished initializing its data
            if (!terrain.IsInitialized)
            {
                if (enableLogging)
                    Debug.Log($"[BuriedObject] Skipping exposure update for '{name}' because terrain isn't initialized yet.");
                return;
            }
            
            if (_exposureDetector == null) InitializeDetector();

            if (autoDetectBounds)
            {
                customBounds = CalculateBounds();
            }

            ExposureData newExposure = _exposureDetector.CalculateExposure(customBounds, terrain);
            
            if (Mathf.Abs(newExposure.Exposure - _currentExposure.Exposure) > 0.01f)
            {
                _currentExposure = newExposure;
                // Use event wrapper to ensure debug is printed when configured
                events.InvokeExposureChanged(_currentExposure);
                
                CheckStateTransitions();
            }
        }
        
        public bool IsFullyExposed()
        {
            return _currentExposure.IsFullyExposed(fullyExposedThreshold);
        }
        #endregion
        
        #region State Management
        private void CheckStateTransitions()
        {
            ExposureState currentState = GetCurrentState();

            // Only fire events on state change
            if (currentState != _previousState)
            {
                switch (currentState)
                {
                    case ExposureState.FullyExposed:
                        OnBecomeFullyExposed();
                        break;

                    case ExposureState.PartiallyExposed:
                        OnBecomePartiallyExposed();
                        break;

                    case ExposureState.Buried:
                        OnBecomeCompletelyBuried();
                        break;
                }

                _previousState = currentState;
            }
        }
        
        private ExposureState GetCurrentState()
        {
            if (_currentExposure.Equals(default(ExposureData)))
            {
                return ExposureState.Buried;
            }
            if (_currentExposure.Exposure >= fullyExposedThreshold)
                return ExposureState.FullyExposed;
            
            if (_currentExposure.Exposure >= partiallyExposedThreshold)
                return ExposureState.PartiallyExposed;
            
            return ExposureState.Buried;
        }
        
        private void OnBecomeFullyExposed()
        {
            if (!_hasBeenFullyExposed)
            {
                _hasBeenFullyExposed = true;
                events.InvokeFullyExposed(gameObject, _currentExposure);
                if (enableLogging)
                    Debug.Log($"{name} has been fully exposed! ({_currentExposure})");
            }
        }
        
        private void OnBecomePartiallyExposed()
        {
            events.InvokePartiallyExposed(gameObject, _currentExposure.Exposure, _currentExposure);
            if (enableLogging)
                Debug.Log($"{name} is partially exposed ({_currentExposure})");
        }

        private void OnBecomeCompletelyBuried()
        {
            events.InvokeCompletelyBuried(_currentExposure);
            if (enableLogging)
                Debug.Log($"{name} has become buried again ({_currentExposure})");
        }
        #endregion
        
        #region Public API
        public void ForceUpdateExposure()
        {
            var terrain = FindFirstObjectByType<VoxelTerrain>();
            if (terrain != null)
            {
                UpdateExposure(terrain);
            }
        }
        
        public void SetExposureDetector(IExposureDetector detector)
        {
            _exposureDetector = detector ?? throw new System.ArgumentNullException(nameof(detector));
        }
        
        public void ResetState()
        {
            _currentExposure = new ExposureData(0f, 0, 0, Position, customBounds);
            _previousState = ExposureState.Buried;
            _hasBeenFullyExposed = false;
        }
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Bounds bounds = autoDetectBounds ? CalculateBounds() : customBounds;

            Color gizmoColor = GetCurrentState() switch
            {
                ExposureState.FullyExposed => exposedColor,
                ExposureState.PartiallyExposed => partialColor,
                _ => buriedColor
            };

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if (!_currentExposure.Equals(default(ExposureData)) && _currentExposure.SampleBounds.size != Vector3.zero)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
                Gizmos.DrawWireCube(_currentExposure.SampleBounds.center, _currentExposure.SampleBounds.size);
            }

#if UNITY_EDITOR
            if (Application.isPlaying && !_currentExposure.Equals(default(ExposureData)))
            {
                UnityEditor.Handles.Label(
                    bounds.center + Vector3.up * bounds.extents.y,
                    $"{name}\n{_currentExposure.Exposure:P0}",
                    new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = new GUIStyleState() { textColor = gizmoColor }
                    }
                );
            }
#endif
        }

        #endregion
        
    }
}
