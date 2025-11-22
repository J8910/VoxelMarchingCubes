using UnityEngine;
using VoxelMarchingCubes.Core.Scaling;

namespace VoxelMarchingCubes.Core.Density
{
    public abstract class ScaleAwareDensityGenerator : IVoxelDensityGenerator
    {
        protected VoxelScaleManager _scaleManager = new VoxelScaleManager();
        protected Vector3 _worldScale = Vector3.one;
        
        public virtual void SetScale(Vector3 scale)
        {
            _worldScale = scale;
            _scaleManager.WorldScale = scale;
            OnScaleChanged(scale);
        }
        
        protected virtual void OnScaleChanged(Vector3 newScale) { }
        
        public abstract float GetDensity(int x, int y, int z);
        
        protected Vector3 VoxelToWorldPosition(int x, int y, int z)
        {
            return _scaleManager.VoxelToWorldPosition(new Vector3(x, y, z));
        }
        
        protected float GetScaledCoordinate(int coordinate, int axis)
        {
            return coordinate / _worldScale[axis];
        }
        
        protected Vector3 GetScaledPosition(int x, int y, int z)
        {
            return new Vector3(
                GetScaledCoordinate(x, 0),
                GetScaledCoordinate(y, 1),
                GetScaledCoordinate(z, 2)
            );
        }
    }
}