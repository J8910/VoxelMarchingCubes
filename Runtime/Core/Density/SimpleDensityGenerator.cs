namespace VoxelMarchingCubes.Core.Density
{
    public class SimpleDensityGenerator : ScaleAwareDensityGenerator
    {
        private readonly float _groundLevel;
        
        public SimpleDensityGenerator(float groundLevel = 0f)
        {
            _groundLevel = groundLevel;
        }
        
        public override float GetDensity(int x, int y, int z)
        {
            float scaledY = GetScaledCoordinate(y, 1);
            return _groundLevel - scaledY;
        }
    }
}