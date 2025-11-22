using NUnit.Framework;
using UnityEngine;
using VoxelMarchingCubes.Core;

namespace VoxelMarchingCubes.Tests.Editor
{
    public class MarchingCubesTests
    {
        [Test]
        public void Generate_OnUniformEmptyGrid_ProducesNoTriangles()
        {
            var grid = new VoxelGrid(new Vector3Int(4, 4, 4));
            // all densities default to 0
            var gen = new MarchingCubesGenerator(0.5f);
            var mesh = gen.Generate(grid);
            Assert.That(mesh.Triangles, Is.Not.Null);
            Assert.That(mesh.Triangles.Length, Is.EqualTo(0));
            Assert.That(mesh.Vertices.Length, Is.EqualTo(0));
            Assert.That(mesh.Normals.Length, Is.EqualTo(0));
        }

        [Test]
        public void Generate_WithSingleHighDensityCorner_ProducesSomeGeometry_AndNormalsMatchCount()
        {
            var grid = new VoxelGrid(new Vector3Int(3, 3, 3));
            // Set a single high-density voxel so that at least one cell has mixed corners
            grid.SetVoxel(new Vector3Int(1, 1, 1), 1.0f); // > iso

            var gen = new MarchingCubesGenerator(0.5f);
            var mesh = gen.Generate(grid);

            Assert.That(mesh.Vertices.Length, Is.GreaterThan(0));
            Assert.That(mesh.Triangles.Length, Is.GreaterThan(0));
            Assert.That(mesh.Normals.Length, Is.EqualTo(mesh.Vertices.Length));

            // Spot check normals are normalized or zero-replaced with up
            for (int i = 0; i < mesh.Normals.Length; i++)
            {
                float mag = mesh.Normals[i].magnitude;
                Assert.That(mag, Is.InRange(0.99f, 1.01f));
            }
        }
    }
}
