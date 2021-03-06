using API.Geo.Cuboid;
using Mogre;

namespace Game.World.Generator
{
    public class VanillaMultiHorizontalEighthBlock : MultiBlock {

        public static Vector2[] textureCoord =
                new Vector2[] {
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
                    new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0),
                    new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1),
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0)
                };
        //see API.generic.BlockFace
        public static Vector3[] blockPointCoords =
            new Vector3[] {
                new Vector3(0, CUBE_SIDE / 8, 0),         new Vector3(0, 0, 0),                              new Vector3(CUBE_SIDE, 0, 0),                      new Vector3(CUBE_SIDE, CUBE_SIDE / 8, 0),
                new Vector3(0, 0, -CUBE_SIDE),            new Vector3(0, CUBE_SIDE  / 8, -CUBE_SIDE),        new Vector3(CUBE_SIDE, CUBE_SIDE / 8, -CUBE_SIDE), new Vector3(CUBE_SIDE, 0, -CUBE_SIDE),
                new Vector3(CUBE_SIDE, CUBE_SIDE / 8, 0), new Vector3(CUBE_SIDE, CUBE_SIDE / 8, -CUBE_SIDE), new Vector3(0, CUBE_SIDE / 8, -CUBE_SIDE),         new Vector3(0, CUBE_SIDE / 8, 0),
                new Vector3(0, 0, 0),                     new Vector3(0, 0, -CUBE_SIDE),                     new Vector3(CUBE_SIDE, 0, -CUBE_SIDE),             new Vector3(CUBE_SIDE, 0, 0),
                new Vector3(0, 0, 0),                     new Vector3(0, CUBE_SIDE / 8, 0),                  new Vector3(0, CUBE_SIDE / 8, -CUBE_SIDE),         new Vector3(0, 0, -CUBE_SIDE),
                new Vector3(CUBE_SIDE, 0, 0),             new Vector3(CUBE_SIDE, 0, -CUBE_SIDE),             new Vector3(CUBE_SIDE, CUBE_SIDE / 8, -CUBE_SIDE), new Vector3(CUBE_SIDE, CUBE_SIDE / 8, 0)
            };

        public static Vector3[] normals =
            new Vector3[] {
                new Vector3(0, 0, -1),
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(-1, 0, 0),
                new Vector3(-1, 0, 0)
            };
        public override Vector3 getBlockPointsCoord(int face) { return blockPointCoords[face]; }
        public override Vector2 getTextureCoord(int face)     { return textureCoord[face];     }
        public override Vector3 getNormals(int face)          { return normals[face];          }

        public VanillaMultiHorizontalEighthBlock(string mat, Island current, API.Geo.World mainWorld, int meshType) : base(mat, current, mainWorld, meshType) { }
    }
}
