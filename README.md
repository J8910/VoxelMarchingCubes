# Voxel Marching Cubes

Core systems and tools for voxel terrain with marching cubes for Unity.

## Features

- Marching Cubes terrain generation
- Voxel-based terrain manipulation
- Adaptive voxel resolution
- Multiple density generators (Perlin noise, improved Perlin, simple density)
- Terrain editing tools (dig, build, paint, smooth, pickaxe)
- Performance profiling utilities

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the **+** button and select "Add package from git URL"
3. Enter: `https://github.com/J8910/VoxelMarchingCubes.git`

To install a specific version, add the version tag:
```
https://github.com/J8910/VoxelMarchingCubes.git#v0.4.1
```

### Option 2: Download Package Archive

1. Go to the [Releases](https://github.com/J8910/VoxelMarchingCubes/releases) page
2. Download the latest `.zip` or `.tar.gz` file
3. Extract the contents into your Unity project's `Packages` folder (for UPM) or `Assets` folder

## Creating a Release

This repository uses GitHub Actions to automatically create Unity package releases.

### Automatic Release (via Git Tag)

1. Update the version in `package.json`
2. Commit the change
3. Create and push a version tag:
   ```bash
   git tag v0.4.1
   git push origin v0.4.1
   ```
4. GitHub Actions will automatically:
   - Build the package archive (zip and tar.gz)
   - Create a GitHub release
   - Upload the package files as release assets

### Manual Release

You can also trigger the workflow manually from the GitHub Actions tab.

## Requirements

- Unity 2022.1 or later

## License

See [LICENSE](LICENSE) file for details.

## Author

Javier Amador ([@J8910](https://github.com/J8910))
