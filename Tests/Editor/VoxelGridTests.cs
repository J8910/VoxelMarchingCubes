using NUnit.Framework;
using UnityEngine;
using VoxelMarchingCubes.Core;

namespace VoxelMarchingCubes.Tests.Editor
{
    public class VoxelGridTests
    {
        [Test]
        public void ModifyDensity_IncreasesWithinRadius_LeavesOutsideUntouched()
        {
            var grid = new VoxelGrid(new Vector3Int(8, 8, 8));
            var center = new Vector3(4, 4, 4);
            float radius = 2f;
            float delta = 0.5f;

            // Baseline: all zeros
            Assert.That(grid.GetDensity(new Vector3Int(4, 4, 4)), Is.EqualTo(0f));

            grid.ModifyDensity(center, radius, delta);

            // At center: should increase close to delta (falloff ~1)
            Assert.That(grid.GetDensity(new Vector3Int(4, 4, 4)), Is.GreaterThan(0.45f));

            // Outside sphere: pick a far corner
            var outside = new Vector3Int(0, 0, 0);
            float outsideDensity = grid.GetDensity(outside);
            Assert.That(outsideDensity, Is.EqualTo(0f));
        }

        [Test]
        public void SetDensityInRadius_BlendsTowardTarget_WithFalloff()
        {
            var grid = new VoxelGrid(new Vector3Int(8, 8, 8));
            // Pre-fill the grid to 0 to clarify changes
            grid.Clear();

            var center = new Vector3(4, 4, 4);
            float radius = 3f;
            float target = 1f;
            float blend = 0.5f;

            grid.SetDensityInRadius(center, radius, target, blend);

            // Center should be halfway to target since falloff ~1 at center
            float centerDensity = grid.GetDensity(new Vector3Int(4, 4, 4));
            Assert.That(centerDensity, Is.InRange(0.45f, 0.55f));

            // A point near the edge of radius should have smaller change than center
            var nearEdge = new Vector3(6.8f, 4, 4); // roughly at radius distance from center in x
            var nearEdgeInt = new Vector3Int(Mathf.RoundToInt(nearEdge.x), 4, 4);
            float edgeDensity = grid.GetDensity(nearEdgeInt);
            Assert.That(edgeDensity, Is.LessThan(centerDensity));
        }

        [Test]
        public void GetAverageDensity_ReturnsZeroOnEmptyGrid()
        {
            var grid = new VoxelGrid(new Vector3Int(4, 4, 4));
            float avg = grid.GetAverageDensity(new Vector3(1.5f, 1.5f, 1.5f), 1.5f);
            Assert.That(avg, Is.EqualTo(0f));
        }
    }
}
