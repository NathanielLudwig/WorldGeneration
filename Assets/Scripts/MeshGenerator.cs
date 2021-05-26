using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;

    private Point[] points;

    public float isoLevel;
    public int numPointsPerAxis = 30;

    public int seed;
    private FastNoiseLite noise;
    private int numTris;
    private Triangle[] triangles;
    // Start is called before the first frame update
    private void Start()
    {
        noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Init();
        CalculateDensity();
        numTris = March();
        UpdateMesh();
    }

    private void Init()
    {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        points = new Point[numPoints];
        triangles = new Triangle[maxTriangleCount];
    }


    private void UpdateMesh()
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[numTris * 3];
        int[] meshTriangles = new int[numTris * 3];
        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
    }

    private void CalculateDensity()
    {
        for (int x = 0; x < numPointsPerAxis; x++)
        {
            for (int y = 0; y < numPointsPerAxis; y++)
            {
                for (int z = 0; z < numPointsPerAxis; z++)
                {
                    float density = -(y-10) + ((noise.GetNoise(x, z) + noise.GetNoise(x, z) + noise.GetNoise(x, z)) * 5);
                    points[IndexFromCoord(x, y, z)] = new Point {pos = new Vector3(x, y, z), density = density};
                }
            }
        }
        
    }

    private int March()
    {
        int ntriang = 0;
        for (int x = 0; x < numPointsPerAxis - 1; x++)
        {
            for (int y = 0; y < numPointsPerAxis -1; y++)
            {
                for (int z = 0; z < numPointsPerAxis - 1; z++)
                {
                    Point[] cubeCorners =
                    {
                        points[IndexFromCoord(x, y, z)],
                        points[IndexFromCoord(x + 1, y, z)],
                        points[IndexFromCoord(x + 1, y, z + 1)],
                        points[IndexFromCoord(x, y, z + 1)],
                        points[IndexFromCoord(x, y + 1, z)],
                        points[IndexFromCoord(x + 1, y + 1, z)],
                        points[IndexFromCoord(x + 1, y + 1, z + 1)],
                        points[IndexFromCoord(x, y + 1, z + 1)]
                    };
                    int cubeIndex = 0;
                    if (cubeCorners[0].density < isoLevel) cubeIndex |= 1;
                    if (cubeCorners[1].density < isoLevel) cubeIndex |= 2;
                    if (cubeCorners[2].density < isoLevel) cubeIndex |= 4;
                    if (cubeCorners[3].density < isoLevel) cubeIndex |= 8;
                    if (cubeCorners[4].density < isoLevel) cubeIndex |= 16;
                    if (cubeCorners[5].density < isoLevel) cubeIndex |= 32;
                    if (cubeCorners[6].density < isoLevel) cubeIndex |= 64;
                    if (cubeCorners[7].density < isoLevel) cubeIndex |= 128;
                    
                    
                    for (int i=0; Table.triTable[cubeIndex, i] !=-1; i+=3) {
                        int a0 = Table.cornerIndexAFromEdge[Table.triTable[cubeIndex, i]];
                        int b0 = Table.cornerIndexBFromEdge[Table.triTable[cubeIndex, i]];

                        int a1 = Table.cornerIndexAFromEdge[Table.triTable[cubeIndex, i+1]];
                        int b1 = Table.cornerIndexBFromEdge[Table.triTable[cubeIndex, i+1]];

                        int a2 = Table.cornerIndexAFromEdge[Table.triTable[cubeIndex, i+2]];
                        int b2 = Table.cornerIndexBFromEdge[Table.triTable[cubeIndex, i+2]];
                        triangles[ntriang].c = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                        triangles[ntriang].b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                        triangles[ntriang].a = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                        ntriang++;
                    }    
                }
            }
        }

        return ntriang;
    }

    private int IndexFromCoord(int x, int y, int z)
    {
        return z * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + x;
    }

    Vector3 InterpolateVerts(Point v1, Point v2) {
        float t = (isoLevel - v1.density) / (v2.density - v1.density);
        return v1.pos + t * (v2.pos-v1.pos);
    }
    struct Point
    {
        public Vector3 pos;
        public float density;
    }

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        
        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
}
