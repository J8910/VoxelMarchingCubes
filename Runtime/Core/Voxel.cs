using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelMarchingCubes.Core
{
    public struct Voxel
    {
        public float Density { get; set; }
        
        public bool IsSolid => Density > 0;
    }
}
