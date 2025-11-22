using UnityEngine;
using VoxelMarchingCubes.Runtime;

namespace VoxelMarchingCubes.Tools
{
    /// <summary>
    /// A directional, focused terrain removal tool that mimics a pickaxe strike.
    /// Unlike shovel-like spherical dig, PickaxeTool applies several small hits
    /// along a short segment in a given direction, forming a chiseled groove.
    /// </summary>
    [System.Serializable]
    public sealed class PickaxeTool : TerrainToolBase
    {
        [SerializeField] private float strikeLength = 1.0f;
        [SerializeField] private int samples = 4;

        // Direction of the strike in world space. Provided by the controller at apply-time.
        private Vector3 _direction = Vector3.forward;

        public PickaxeTool() : this(0.6f, 1f, 1.0f, 4) { }

        public PickaxeTool(float radius, float intensity, float length = 1.0f, int samples = 4) : base(radius, intensity)
        {
            this.strikeLength = Mathf.Max(0.01f, length);
            this.samples = Mathf.Clamp(samples, 1, 16);
        }

        /// <summary>
        /// Sets the strike direction used for the next Apply call.
        /// </summary>
        public void SetDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude > 1e-6f)
                _direction = worldDirection.normalized;
        }

        /// <summary>
        /// Configure strike length in world units.
        /// </summary>
        public void SetStrikeLength(float length)
        {
            strikeLength = Mathf.Max(0.01f, length);
        }

        /// <summary>
        /// Configure number of samples along strike path.
        /// </summary>
        public void SetSamples(int count)
        {
            samples = Mathf.Clamp(count, 1, 32);
        }

        public override void Apply(IModificableVoxel terrain, Vector3 worldPosition)
        {
            if (terrain == null) return;

            float removeDelta = -Mathf.Abs(intensity);
            Vector3 dir = _direction.sqrMagnitude > 0f ? _direction : Vector3.forward;

            // Distribute smaller hits along the strike path with mild radius falloff.
            for (int i = 0; i < samples; i++)
            {
                float t = samples <= 1 ? 0f : (float)i / (samples - 1);
                Vector3 p = worldPosition + dir * (t * strikeLength);
                float r = Mathf.Lerp(radius, radius * 0.5f, t); // narrower towards the end
                float delta = Mathf.Lerp(removeDelta, removeDelta * 0.75f, t); // slightly weaker along stroke

                if (TryGetVoxelTerrain(terrain, out var voxelTerrain))
                {
                    voxelTerrain.ModifyTerrain(p, r, delta);
                }
                else
                {
                    terrain.ModifyTerrain(p, r, delta);
                }
            }
        }
    }
}
