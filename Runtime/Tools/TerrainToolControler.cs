using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    [RequireComponent(typeof(SphereCollider))]
    public class TerrainToolController : MonoBehaviour
    {
        #region Constants
        
        private const float MinToolRadius = 0.0001f;
        private const float MinToolIntensity = 0.01f;
        private const float MinSmoothStrength = 0f;
        private const float MaxSmoothStrength = 1f;
        private const float MinActionCooldown = 0f;
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Tool Selection")]
        [SerializeField] private ToolType currentToolType = ToolType.Dig;
        
        [Header("Tool Settings")]
        [SerializeField] private float toolRadius = 2f;
        [SerializeField] private float toolIntensity = 1f;
        [SerializeField] private float smoothStrength = 0.5f;
        [SerializeField, Tooltip("Pickaxe strike length in world units")] private float pickaxeLength = 1.0f;
        [SerializeField, Tooltip("Samples along the pickaxe strike path")] private int pickaxeSamples = 4;
        
        [Header("Pickaxe Momentum")]
        [SerializeField, Tooltip("If enabled, the pickaxe only applies when the controller is moving fast enough (momentum strike)")]
        private bool pickaxeRequiresMomentum = true;
        [SerializeField, Tooltip("Minimum controller speed required to register a pickaxe strike (in world units per second)")]
        private float minPickaxeSpeed = 0.5f;
        [SerializeField, Tooltip("Require motion in the tool's forward direction to count as a strike")] 
        private bool requireForwardMotion = true;
        
        [Header("Scale Adaptation")]
        [Tooltip("Automatically scale tool parameters based on terrain scale")]
        [SerializeField] private bool adaptToTerrainScale = true;
        [Tooltip("Minimum intensity regardless of scale (prevents tools from being too weak)")]
        [SerializeField] private float minIntensity = 0.001f;

        [Header("Interaction")]
        [SerializeField] private InteractionMode interactionMode = InteractionMode.OnInput;
        [SerializeField] private KeyCode primaryActionKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode secondaryActionKey = KeyCode.Mouse1;
        [SerializeField] private float actionCooldown = 0.05f; // Prevent spam
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showToolPreview = true;
        [SerializeField] private Color digColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color buildColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color smoothColor = new Color(0f, 0.5f, 1f, 0.5f);
        [SerializeField] private Color pickaxeColor = new Color(1f, 0.5f, 0f, 0.6f);
        [SerializeField] private Color paintColor = new Color(1f, 1f, 0f, 0.6f);
        
        [Header("Advanced")]
        [SerializeField] private LayerMask terrainLayerMask = -1;
        [SerializeField] private bool respectScale = true;
        
        [Header("Debugging")]
        [Tooltip("Enable verbose runtime logging for tool interactions and application.")]
        [SerializeField] private bool enableLogging = false;
        
        #endregion
        
        #region Private Fields
        
        private SphereCollider _triggerCollider;
        private ITerrainTools _currentTool;
        private IModificableVoxel _targetTerrain;
        private float _lastActionTime;
        private bool _isInTerrainZone;
        
        // Motion tracking for momentum-based pickaxe
        private Vector3 _lastPosition;
        private Vector3 _smoothedVelocity;
        private bool _hasLastPosition;
        
        // Tool instances (cached for performance)
        private DigTool _digTool;
        private BuildTool _buildTool;
        private SmoothTool _smoothTool;
        private PickaxeTool _pickaxeTool;
        private PaintTool _paintTool;
        
        #endregion
        
        #region Enums
        
        public enum ToolType
        {
            Dig,
            Build,
            Smooth,
            Pickaxe,
            Paint
        }

        public enum InteractionMode
        {
            Automatic,      // Constantly applies when in range
            OnInput,        // Requires key press
            SingleClick     // One click = one action
        }
        
        #endregion
        
        #region Properties
        
        public ToolType CurrentToolType
        {
            get => currentToolType;
            set
            {
                if (currentToolType != value)
                {
                    currentToolType = value;
                    UpdateCurrentTool();
                }
            }
        }

        public float ToolRadius
        {
            get => toolRadius;
            set
            {
                toolRadius = Mathf.Max(MinToolRadius, value);
                UpdateToolParameters();
                UpdateColliderSize();
            }
        }

        public float ToolIntensity
        {
            get => toolIntensity;
            set
            {
                toolIntensity = Mathf.Max(MinToolIntensity, value);
                UpdateToolParameters();
            }
        }

        public bool IsActive => _isInTerrainZone && _targetTerrain != null;
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            InitializeTools();
            UpdateCurrentTool();
        }

        private void Update()
        {
            UpdateMotion();
            HandleInput();
        }

        private void OnValidate()
        {
            toolRadius = Mathf.Max(MinToolRadius, toolRadius);
            toolIntensity = Mathf.Max(MinToolIntensity, toolIntensity);
            smoothStrength = Mathf.Clamp01(smoothStrength);
            actionCooldown = Mathf.Max(MinActionCooldown, actionCooldown);
            pickaxeLength = Mathf.Max(0.01f, pickaxeLength);
            pickaxeSamples = Mathf.Clamp(pickaxeSamples, 1, 32);
            minPickaxeSpeed = Mathf.Max(0f, minPickaxeSpeed);
            
            if (Application.isPlaying)
            {
                UpdateToolParameters();
                UpdateColliderSize();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (enableLogging)
                Debug.Log($"<color=blue>[TerrainToolController]</color> OnTriggerEnter: {other.name}");
                
            if (IsValidTerrain(other))
            {
                _targetTerrain = other.GetComponent<IModificableVoxel>();
                _isInTerrainZone = true;
                
                if (enableLogging)
                    Debug.Log($"<color=green>[TerrainToolController]</color> ✓ Terrain detected: {other.name}");
            }
            else
            {
                if (enableLogging)
                    Debug.Log($"<color=yellow>[TerrainToolController]</color> Not a valid terrain: {other.name}");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsValidTerrain(other))
                return;

            // Update target in case it changed
            if (_targetTerrain == null)
            {
                _targetTerrain = other.GetComponent<IModificableVoxel>();
                _isInTerrainZone = true;
            }

            // Automatic mode
            if (interactionMode == InteractionMode.Automatic)
            {
                ApplyToolIfReady();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsValidTerrain(other))
            {
                if (enableLogging)
                    Debug.Log($"<color=yellow>[TerrainToolController]</color> Exiting terrain zone");
                _targetTerrain = null;
                _isInTerrainZone = false;
                _hasLastPosition = false; // reset momentum tracking when leaving terrain
            }
        }

        #endregion
        
        #region Initialization

        private void InitializeComponents()
        {
            _triggerCollider = GetComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            UpdateColliderSize();
        }

        private void InitializeTools()
        {
            _digTool = new DigTool(toolRadius, toolIntensity);
            _buildTool = new BuildTool(toolRadius, toolIntensity);
            _smoothTool = new SmoothTool(toolRadius, smoothStrength);
            _pickaxeTool = new PickaxeTool(toolRadius * 0.5f, toolIntensity, pickaxeLength, pickaxeSamples);
            _paintTool = new PaintTool(toolRadius, Mathf.Clamp01(toolIntensity));
        }

        private void UpdateCurrentTool()
        {
            _currentTool = currentToolType switch
            {
                ToolType.Dig => _digTool,
                ToolType.Build => _buildTool,
                ToolType.Smooth => _smoothTool,
                ToolType.Pickaxe => _pickaxeTool,
                ToolType.Paint => _paintTool,
                _ => _digTool
            };
            
            UpdateToolParameters();
        }

        #endregion
        
        #region Tool Management

        /// <summary>
        /// Switch to a specific tool type
        /// </summary>
        public void SetTool(ToolType toolType)
        {
            CurrentToolType = toolType;
        }

        /// <summary>
        /// Cycle to next tool
        /// </summary>
        public void CycleToNextTool()
        {
            int nextIndex = ((int)currentToolType + 1) % System.Enum.GetValues(typeof(ToolType)).Length;
            CurrentToolType = (ToolType)nextIndex;
        }

        /// <summary>
        /// Update all tool parameters from current settings
        /// </summary>
        private void UpdateToolParameters()
        {
            if (_digTool != null)
            {
                _digTool.SetRadius(toolRadius);
                _digTool.SetIntensity(toolIntensity);
            }

            if (_buildTool != null)
            {
                _buildTool.SetRadius(toolRadius);
                _buildTool.SetIntensity(toolIntensity);
            }

            if (_smoothTool != null)
            {
                _smoothTool.SetRadius(toolRadius);
                _smoothTool.SetStrength(smoothStrength);
            }

            if (_pickaxeTool != null)
            {
                _pickaxeTool.SetRadius(Mathf.Max(0.1f, toolRadius * 0.5f));
                _pickaxeTool.SetIntensity(toolIntensity);
                _pickaxeTool.SetStrikeLength(pickaxeLength);
                _pickaxeTool.SetSamples(pickaxeSamples);
            }

            if (_paintTool != null)
            {
                _paintTool.SetRadius(toolRadius);
                _paintTool.SetIntensity(Mathf.Clamp01(toolIntensity));
            }
        }

        private void UpdateColliderSize()
        {
            if (_triggerCollider != null)
            {
                _triggerCollider.radius = toolRadius;
            }
        }

        #endregion
        
        #region Input Handling

        private void HandleInput()
        {
            if (!_isInTerrainZone || _targetTerrain == null)
                return;

            switch (interactionMode)
            {
                case InteractionMode.OnInput:
                    if (InputAdapter.GetKey(primaryActionKey))
                    {
                        ApplyToolIfReady();
                    }
                    break;

                case InteractionMode.SingleClick:
                    if (InputAdapter.GetKeyDown(primaryActionKey))
                    {
                        ApplyToolIfReady();
                    }
                    break;
            }

            // Secondary action (e.g., tool switching)
            if (InputAdapter.GetKeyDown(secondaryActionKey))
            {
                CycleToNextTool();
            }
        }

        #endregion
        
        #region Tool Application

        /// <summary>
        /// Apply current tool to terrain if cooldown allows
        /// </summary>
        private void ApplyToolIfReady()
        {
            if (!CanApplyTool())
            {
                if (enableLogging)
                    Debug.Log($"<color=gray>[TerrainToolController]</color> Cooldown active, skipping");
                return;
            }

            // Momentum gate for pickaxe
            if (currentToolType == ToolType.Pickaxe && pickaxeRequiresMomentum)
            {
                if (!HasPickaxeMomentum())
                {
                    if (enableLogging)
                    {
                        float speed = _smoothedVelocity.magnitude;
                        float forwardSpeed = Vector3.Dot(_smoothedVelocity, transform.forward);
                        Debug.Log($"<color=gray>[TerrainToolController]</color> Pickaxe momentum insufficient (speed={speed:F2}, fwd={forwardSpeed:F2}, min={minPickaxeSpeed:F2})");
                    }
                    return; // do not consume cooldown
                }
            }

            ApplyTool();
            _lastActionTime = Time.time;
        }

        /// <summary>
        /// Check if tool can be applied (respecting cooldown)
        /// </summary>
        private bool CanApplyTool()
        {
            return Time.time >= _lastActionTime + actionCooldown;
        }

        /// <summary>
        /// Apply the current tool to the target terrain
        /// </summary>
        private void ApplyTool()
        {
            if (_currentTool == null)
            {
                Debug.LogError("<color=red>[TerrainToolController]</color> Current tool is NULL!");
                return;
            }
                
            if (_targetTerrain == null)
            {
                Debug.LogError("<color=red>[TerrainToolController]</color> Target terrain is NULL!");
                return;
            }
            
            Vector3 applicationPoint = GetApplicationPoint();
            
            float effectiveRadius = toolRadius;
            float effectiveIntensity = toolIntensity;
                
            if (enableLogging)
                Debug.Log($"<color=green>[TerrainToolController]</color> ★ Preparing to apply {currentToolType} at {applicationPoint}");
                
            if (adaptToTerrainScale && _targetTerrain is MonoBehaviour mb)
                {
                    VoxelTerrain terrain = mb.GetComponentInParent<VoxelTerrain>();
                    if (terrain == null) terrain = mb as VoxelTerrain;
                    
                    if (terrain != null)
                    {
                        Vector3 scale = terrain.transform.lossyScale;
                        float avgScale = (scale.x + scale.y + scale.z) / 3f;
                        
                        effectiveRadius = toolRadius * avgScale;
                        
                        effectiveIntensity = Mathf.Max(minIntensity, toolIntensity * Mathf.Sqrt(avgScale));
                        
                        if (enableLogging)
                            Debug.Log($"<color=cyan>[TerrainToolController]</color> Scale adaptation: " +
                                      $"scale={avgScale:F3}, radius={toolRadius:F2}→{effectiveRadius:F2}, " +
                                      $"intensity={toolIntensity:F2}→{effectiveIntensity:F2}");
                    }
                }

                // Respect the controller's own transform scale if enabled
                if (respectScale)
                {
                    var s = transform.lossyScale;
                    float selfAvg = (s.x + s.y + s.z) / 3f;
                    effectiveRadius *= selfAvg;
                }
                if (enableLogging)
                    Debug.Log($"<color=green>[TerrainToolController]</color> ★ Applying {currentToolType} at {applicationPoint} (r={effectiveRadius:F2}, i={effectiveIntensity:F2})");
                
                float originalRadius = _currentTool.GetRadius();
                _currentTool.SetRadius(effectiveRadius);
                
                // Directional data for pickaxe
                float originalIntensityBackup = toolIntensity;
                float originalStrikeLen = pickaxeLength;

                if (_currentTool is TerrainToolBase toolBaseCommon)
                {
                    toolBaseCommon.SetIntensity(effectiveIntensity);

                    if (_currentTool is PickaxeTool pickaxe)
                    {
                        // Scale strike length with terrain scale if enabled
                        float effectiveLength = pickaxeLength;
                        if (adaptToTerrainScale && _targetTerrain is MonoBehaviour mb2)
                        {
                            var vt = mb2.GetComponentInParent<VoxelTerrain>() ?? mb2 as VoxelTerrain;
                            if (vt != null)
                            {
                                Vector3 s = vt.transform.lossyScale;
                                float avg = (s.x + s.y + s.z) / 3f;
                                effectiveLength = pickaxeLength * avg;
                            }
                        }
                        // Additionally respect the controller's scale if requested
                        if (respectScale)
                        {
                            var self = transform.lossyScale;
                            float selfAvg = (self.x + self.y + self.z) / 3f;
                            effectiveLength *= selfAvg;
                        }
                        pickaxe.SetStrikeLength(effectiveLength);
                        pickaxe.SetDirection(transform.forward);
                    }

                    _currentTool.Apply(_targetTerrain, applicationPoint);

                    // Restore intensity for tool instances
                    toolBaseCommon.SetIntensity(originalIntensityBackup);
                    if (_currentTool is PickaxeTool)
                    {
                        // restore pickaxe configured length
                        if (_pickaxeTool != null) _pickaxeTool.SetStrikeLength(originalStrikeLen);
                    }
                }
                else
                {
                    _currentTool.Apply(_targetTerrain, applicationPoint);
                }
                
                _currentTool.SetRadius(originalRadius); 
        }

        /// <summary>
        /// Get the world position where tool should be applied
        /// </summary>
        private Vector3 GetApplicationPoint()
        {
            return transform.position;
        }

        /// <summary>
        /// Apply tool manually (can be called from external scripts)
        /// </summary>
        public void ApplyToolManual()
        {
            if (_targetTerrain != null)
            {
                ApplyTool();
            }
        }

        #endregion
        
        #region Motion & Momentum

        private void UpdateMotion()
        {
            if (!_isInTerrainZone)
            {
                // When not in terrain, still track to allow a strike just as we enter
                // but we avoid gating by early returns elsewhere
            }

            Vector3 currentPos = transform.position;
            if (!_hasLastPosition)
            {
                _lastPosition = currentPos;
                _smoothedVelocity = Vector3.zero;
                _hasLastPosition = true;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Vector3 rawVelocity = (currentPos - _lastPosition) / dt;
            _lastPosition = currentPos;

            // Exponential smoothing that is stable across frame rates
            float smoothFactor = 1f - Mathf.Exp(-10f * dt); // ~0.86 at 60 FPS
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, rawVelocity, smoothFactor);
        }

        private bool HasPickaxeMomentum()
        {
            if (!requireForwardMotion)
            {
                return _smoothedVelocity.magnitude >= minPickaxeSpeed;
            }

            float forwardSpeed = Vector3.Dot(_smoothedVelocity, transform.forward);
            return forwardSpeed >= minPickaxeSpeed;
        }

        #endregion
        
        #region Validation

        private bool IsValidTerrain(Collider other)
        {
            // Check layer mask
            if (terrainLayerMask != (terrainLayerMask | (1 << other.gameObject.layer)))
                return false;

            // Check if has required interface
            return other.GetComponent<IModificableVoxel>() != null;
        }

        #endregion
        
        #region Public API

        /// <summary>
        /// Set radius and intensity at once
        /// </summary>
        public void SetToolParameters(float radius, float intensity)
        {
            ToolRadius = radius;
            ToolIntensity = intensity;
        }

        /// <summary>
        /// Get info about current tool
        /// </summary>
        public string GetToolInfo()
        {
            return $"Tool: {currentToolType} | Radius: {toolRadius:F2} | Intensity: {toolIntensity:F2}";
        }

        #endregion
        
        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showToolPreview)
                return;

            Color gizmoColor = currentToolType switch
            {
                ToolType.Dig => digColor,
                ToolType.Build => buildColor,
                ToolType.Smooth => smoothColor,
                ToolType.Pickaxe => pickaxeColor,
                ToolType.Paint => paintColor,
                _ => Color.white
            };

            // Outer sphere (tool range)
            Gizmos.color = gizmoColor;
            float radius = GetEffectiveRadius();
            Gizmos.DrawWireSphere(transform.position, radius);

            // Inner sphere (center indicator)
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.5f);
            Gizmos.DrawSphere(transform.position, radius * 0.1f);

            // Connection line to terrain (if active)
            if (_isInTerrainZone && _targetTerrain != null)
            {
                Gizmos.color = Color.green;
                if (_targetTerrain is MonoBehaviour terrainMono)
                {
                    Gizmos.DrawLine(transform.position, terrainMono.transform.position);
                }
            }

            // Tool type label (editor only)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * radius * 1.2f,
                $"{currentToolType}\nR:{toolRadius:F1} I:{toolIntensity:F1}",
                new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = gizmoColor }
                }
            );
#endif
        }

        private float GetEffectiveRadius()
        {
            float baseRadius = Application.isPlaying ? 
                (_currentTool?.GetRadius() ?? toolRadius) : 
                toolRadius;

            if (respectScale)
            {
                var s = transform.lossyScale;
                float avg = (s.x + s.y + s.z) / 3f;
                baseRadius *= Mathf.Max(0.0001f, avg);
            }
            return baseRadius;
        }

        #endregion
    }
}