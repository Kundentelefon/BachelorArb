using UnityEngine;
using UnityEngine.Assertions.Comparers;

public static class MeshGenerator
{

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurveDiag, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(heightCurveDiag.keys); //solves the problem with threadening. each curve has its own height curve

        int meshSimplifierIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; //if LOD = 0 meshsimplificationIncrement = 1

        int borderSize = heightMap.GetLength(0); //width and heigth dont needed, because we work with squares
        int meshSize = borderSize - 2 * meshSimplifierIncrement; //so borders are even correct for lower LOD chunks
        int meshSizeUnsimpled = borderSize - 2; //bordersize is always 2 points bigger than the meshsize. example 3 points edge has 5 points edge border

        float topLeftX = (meshSizeUnsimpled - 1) / -2f; //centers the mesh in the screen. x in the middle has 0, left has -1 and right 1. for the left one: x=(w-1)/2: meshSizeUnsimplified - 1 so its not dependend on the LOD of Mesh and remains constant
        float topLeftZ = (meshSizeUnsimpled - 1) / 2f;
        
        int verticesPerLine = (meshSize - 1) / meshSimplifierIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] verticesIndicesMap = new int[borderSize, borderSize];  //creates 2D array of bordersize. example -1 to -16
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        //creates the borderindex as -1 and so on vertices
        for (int y = 0; y < borderSize; y += meshSimplifierIncrement)
        {
            for (int x = 0; x < borderSize; x += meshSimplifierIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderSize - 1 || x == 0 || x == borderSize - 1;

                if (isBorderVertex)
                {
                    verticesIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    verticesIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        //for loops keeps track where we are at the 1D array of vertices
        for (int y = 0; y < borderSize; y += meshSimplifierIncrement)
        {
            for (int x = 0; x < borderSize; x += meshSimplifierIncrement)
            {
                int vertexIndex = verticesIndicesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplifierIncrement) / (float)meshSize, (y - meshSimplifierIncrement) / (float)meshSize);

                if (float.IsNaN(heightMap[x, y]))
                    heightMap[x, y] = 0;

                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimpled, height, topLeftZ - percent.y * meshSizeUnsimpled); //x in the middle has 0, left has -1 and right 1. 

                meshData.AddVertex(vertexPosition, percent, vertexIndex); //call the addvertex method on the mesh data for borders

                if (x < borderSize - 1 && y < borderSize - 1) // -1 ignores the right and bottom edge vertices of the map to stay inside
                {
                    //goes through vertices and makes a triangle out of them. i, i+w, i+w+1 (first triangle), i, i+1, i+w+1 (second triangle)
                    int a = verticesIndicesMap[x, y]; // x;y
                    int b = verticesIndicesMap[x + meshSimplifierIncrement, y]; // x+i;y
                    int c = verticesIndicesMap[x, y + meshSimplifierIncrement]; // x;y+i
                    int d = verticesIndicesMap[x + meshSimplifierIncrement, y + meshSimplifierIncrement]; //x+i;y+i
                     //creating 2 triangles aout of abcd.
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++; //keeps track where index is for vertices
            }
        }

        meshData.ThreadNormals();

        return meshData; //meshdata instead of Mesh so threating can implemented

    }

}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;  //add textures to the mesh
    Vector3[] threadNormals; //calculates normals in seperate thread instead of mainGamethread

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    //creates Meshdata
    //vertices per Line because we work with a single size value and not with width and heights anymore
    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine]; //number of vertices. width * heights
        uvs = new Vector2[verticesPerLine * verticesPerLine]; //tell each vertex where it is in relation to the rest of the map as percentage of x and y axes ( written in other line)
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6]; // length of triangle array. first counts squares. each square is made of 3 vertices each = 6

        //invisible border for better mesh seams on junk edges
        borderVertices = new Vector3[verticesPerLine * 4 + 4]; // *4 for each side + 4 for each corner
        borderTriangles = new int[24 * verticesPerLine]; //records the indices of each of the 6 vertices which makes the 2 triangles per square between each borderline and mesh. 4*number of vertices per line in the mesh = number of squares = 6 * 4 = 24 as inizialize
    }
    
    //because vertices or uvs cant be add directly, this method needed
    public void AddVertex(Vector3 vertexPos, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0) // less than zero = borderVertex
        {
            borderVertices[-vertexIndex - 1] = vertexPos; //add to borderindex array. to get an appropiate index, start with -1
        }
        else //otherwise its a regular vertex
        {
            vertices[vertexIndex] = vertexPos;
            uvs[vertexIndex] = uv;
        }
    }

    //calculates the mesh triangles of the vertices.
    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0) //if any of the vertices which makes up the triangle is a border vertex
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3; //keeps track of the triangle index
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    //Get rids of the seams between Chunks
    //Mesh of 4 Triangles. Each Triangle has a normal which is perpendicular on it. But unity has a normal on each corner/vertex. The normal is an average of each of its adjecent triangle normals. But on the edge it does not have the information of its adjecent mesh, so the normals are wrongly calculated and the lightning goes wrong and makes a seam 
    Vector3[] CalculateNormals()
    {
        //when generating the mesh junk we also calculate the vertices bordering the mesh. this border will be excluded from the final mesh. its only there to help calculating the mesh. the border goes from -1 downwards. example 3x3 mesh has -1 to -16 border points. every index less than 0 will be excluded from the final mesh
        //bordersize is always 2 points bigger than the meshsize. example 3 points edge has 5 points edge border
        Vector3[] vertexNormals = new Vector3[vertices.Length]; //store the normals in it
        int triangleNrCount = triangles.Length / 3; //how many triangles we have. triangles array has a set of 3 vertices so / 3
        for (int i = 0; i < triangleNrCount; i++)
        {
            int normalTriangleIndex = i * 3; //gets index in triangles array
            int vertexIndexA = triangles[normalTriangleIndex]; //gets the indices of all of the vertices, that make up the current triangle
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormals = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC); //gets the normal vector of the 3 vertices indeces
            vertexNormals[vertexIndexA] += triangleNormals; //add the triangle normal to each of the vertices that are part of the triangle
            vertexNormals[vertexIndexB] += triangleNormals;
            vertexNormals[vertexIndexC] += triangleNormals;
            //after that lightning works but still seams. to get fully rid of the seams look bellow
        }

        //loops through the triangles belongs to the border
        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            //makes sure that the indecis exists in the vertex normals array
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }


        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;

    }

    //returns the normal vector of the 3 vertices indeces
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        //check inf the index is less than zero, if so then get it from the border index array
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        //to calculate the surface normals we use cross product
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ThreadNormals()
    {
        threadNormals = CalculateNormals();
    }

    //Getting the Mesh from the MeshData
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals  = threadNormals; //anti seam 
        return mesh;
    }

}