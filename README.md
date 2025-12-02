**This project is currently under active development**

# Voxel Marching Cubes

A complete Unity package for creating dynamic, editable voxel terrain using the Marching Cubes algorithm. 

![Unity Version](https://img.shields.io/badge/Unity-2022.1%2B-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- **Marching Cubes Implementation**: Smooth voxel terrain generation with configurable resolution
- **Runtime Terrain Editing**: Full suite of tools for modifying terrain in real-time
- **Chunk-based System**: Optimized performance with dynamic chunk management
- **Runtime Editing Tools**:
- **Extensible Architecture**: Interface-based design for custom density generators and mesh generators
- **Buried Objects System**: Logic for objects embedded within terrain
- **Performance Profiling**: Built-in profiling tools for optimization

## Installation

### Via Unity Package Manager

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/J8910/VoxelMarchingCubes.git`

## Quick Start

### Basic Setup

```csharp
using J8910.VoxelMarchingCubes;

// Create a VoxelTerrain component
VoxelTerrain terrain = gameObject.AddComponent<VoxelTerrain>();

// Configure terrain settings
terrain.ChunkSize = 16;
terrain.VoxelSize = 1f;

// Initialize terrain
terrain.Initialize();
```

### Using Terrain Tools

```csharp
using J8910.VoxelMarchingCubes.Tools;

// Setup the terrain tool controller
TerrainToolController toolController = gameObject.AddComponent<TerrainToolController>();

```

## Architecture

### Core Components

- **VoxelTerrain**: Main component managing the entire voxel system
- **VoxelChunk**: Individual terrain chunks with mesh generation
- **VoxelGrid**: 3D grid storing voxel density data
- **MarchingCubesGenerator**: Converts voxel data to mesh using lookup tables

### Extensibility

Implement custom density generators:

```csharp
public class MyDensityGenerator : IVoxelDensityGenerator
{
    public float GenerateDensity(Vector3 position)
    {
        // Your custom terrain generation logic
        return Mathf. PerlinNoise(position. x * 0.1f, position.z * 0.1f);
    }
}
```

## Use Cases

- **Terrain Sculpting**: Real-time terrain editing tools
- **Procedural Worlds**: Dynamic world generation with smooth surfaces
- **Mining Games**: Implement digging and building mechanics

## Performance

The system uses a chunk-based approach to optimize rendering and updates:
- Only modified chunks are regenerated
- Efficient mesh generation using lookup tables
- Profiling markers for performance monitoring

## Advanced Features

### Custom Mesh Generators

Implement `IMeshGenerator` to create custom mesh generation logic beyond Marching Cubes. 

### Modifiable Voxels

Use `IModificableVoxel` interface to create interactive voxel objects that can be modified at runtime.

### Inspector Integration

Built-in inspector tools via `IVoxelTerrainInspectorData` for debugging and visualization. 

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Javier Amador**
- GitHub: [@J8910](https://github.com/J8910)
