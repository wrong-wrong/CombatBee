using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CombatBee
{
/*    public struct GridInfoCOMCB : IComponentData
    {
        public int GridXCount;
        public int GridYCount;
        public float CellXSize;
        public float CellYSize;
        public float2 minGridPos;
    }*/
    public struct StackHeightCOMCB : IBufferElementData
    {
        public int stackHeight; // length should be gridXCount * gridYCount
    }
    /*//[BurstCompile]
    public class GridSimpleFunction
    {
        static int GridXCount;
        static int GridYCount;
        static float CellXSize;
        static float CellYSize;
        static float MinGridPosX;
        static float MinGridPosY;
        static float FieldYSize;
        static float ResourceSize;
        public static void Init(int gridXCount, int gridYCount, float cellXSize, float cellYSize, float minGridPosX, float minGridPosY, float fieldYSize, float resourceSize)
        {
            GridXCount = gridXCount;
            GridYCount = gridYCount;
            CellXSize = cellXSize;
            CellYSize = cellYSize;
            MinGridPosX = minGridPosX;
            MinGridPosY = minGridPosY;
            FieldYSize = fieldYSize;
            ResourceSize = resourceSize;
        }




        //[BurstCompile]
        public static int GridPosToIdx(int gridX, int gridY)
        {
            return gridY * GridXCount + gridX;
        }

        //[BurstCompile]
        public static void GetGridIndex(float3 pos, out int gridX, out int gridY)
        {
            gridX = (int)math.floor((pos.x - MinGridPosX + CellXSize * .5f) / CellXSize);
            gridY = (int)math.floor((pos.z - MinGridPosY + CellYSize * .5f) / CellYSize);

            gridX = math.clamp(gridX, 0, GridXCount - 1);
            gridY = math.clamp(gridY, 0, GridYCount - 1);
        }
        //[BurstCompile]
        public static float GetStackPos(int height)     //differ from the sample, since only the y value of Position is used, we only cal y
        {
            return -FieldYSize * .5f + (height + .5f) * ResourceSize;
        }
        //[BurstCompile]
        public static float3 NearestSnappedPos(float3 pos)
        {
            int x, y;
            GetGridIndex(pos, out x, out y);
            return new float3(MinGridPosX + x * CellXSize, pos.y, MinGridPosY + y * CellYSize);
        }
*//*        public static float3 GridPosAndStackHeightToFloat3(int gridX, int gridY, int height)
        {
            return new float3(MinGridPosX + gridX * CellXSize, GetStackPos(height), MinGridPosY + gridY * CellYSize);
        }*//*
    }*/
}