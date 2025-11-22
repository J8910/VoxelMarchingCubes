using UnityEngine;

namespace VoxelMarchingCubes.Core.Scaling
{
    [System.Serializable]
    public class VoxelScaleManager
    {
        [SerializeField] private float _worldToVoxelScale = 1f;
        [SerializeField] private bool _useUniformScaling = true;
        [SerializeField] private Vector3 _voxelSpacing = Vector3.one;
        
        private Vector3 _cachedScale = Vector3.one;
        private Matrix4x4 _worldToVoxelMatrix = Matrix4x4.identity;
        private Matrix4x4 _voxelToWorldMatrix = Matrix4x4.identity;
        private bool _matricesDirty = true;
        
        public Vector3 WorldScale
        {
            get => _cachedScale;
            set
            {
                if (_cachedScale != value)
                {
                    _cachedScale = value;
                    _matricesDirty = true;
                    UpdateScaleParameters();
                }
            }
        }
        
        public float WorldToVoxelScale => _worldToVoxelScale;
        public Vector3 VoxelSpacing => _voxelSpacing;
        
        private void UpdateScaleParameters()
        {
            if (_useUniformScaling)
            {
                _worldToVoxelScale = 1f / Mathf.Max(_cachedScale.x, _cachedScale.y, _cachedScale.z);
                _voxelSpacing = new Vector3(1f / _cachedScale.x, 1f / _cachedScale.y, 1f / _cachedScale.z);
            }
            else
            {
                _voxelSpacing = new Vector3(1f / _cachedScale.x, 1f / _cachedScale.y, 1f / _cachedScale.z);
                _worldToVoxelScale = (_voxelSpacing.x + _voxelSpacing.y + _voxelSpacing.z) / 3f;
            }
        }
        
        public Vector3 WorldToVoxelPosition(Vector3 worldPos)
        {
            UpdateMatricesIfNeeded();
            return _worldToVoxelMatrix.MultiplyPoint3x4(worldPos);
        }
        
        public Vector3 VoxelToWorldPosition(Vector3 voxelPos)
        {
            UpdateMatricesIfNeeded();
            return _voxelToWorldMatrix.MultiplyPoint3x4(voxelPos);
        }
        
        public float WorldToVoxelDistance(float worldDistance)
        {
            return worldDistance * _worldToVoxelScale;
        }
        
        public float VoxelToWorldDistance(float voxelDistance)
        {
            return voxelDistance / _worldToVoxelScale;
        }
        
        private void UpdateMatricesIfNeeded()
        {
            if (!_matricesDirty) return;
            
            _worldToVoxelMatrix = Matrix4x4.Scale(_voxelSpacing);
            _voxelToWorldMatrix = Matrix4x4.Scale(_cachedScale);
            _matricesDirty = false;
        }
        
        public void LogScaleInfo(string context = "")
        {
            Debug.Log($"=== VoxelScaleManager Debug {context} ===");
            Debug.Log($"World Scale: {_cachedScale}");
            Debug.Log($"World to Voxel Scale: {_worldToVoxelScale}");
            Debug.Log($"Voxel Spacing: {_voxelSpacing}");
            Debug.Log($"Use Uniform Scaling: {_useUniformScaling}");
        }
    }
}