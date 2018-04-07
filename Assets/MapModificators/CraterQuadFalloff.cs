using UnityEngine;
using System.Collections;

public class CraterQuadFalloffGenerator {

    //substracts from noise so landmass is fully sorrounded
    public static float[,] GenerateCraterQuadFalloff(int chunkSize, float xPos, float yPos, float intensity, int direction)
    {
        //direction: 0 = top, 1 = right, 2 = bottom, 3 = left, 4 = all sides
        float[,] map = new float[chunkSize, chunkSize];

        float x;
        float y;
        float value = 0;

        //i and j is coordinate of a point inside the square map
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                //take the coordinates and make them in a range from -1 to 1
                x = ((i + xPos) / (float)chunkSize * 2) - 1;
                y = ((j + yPos) / (float)chunkSize * 2) - 1;

                //get the value to use for map, find out which one, x or y, is closest to the edge of the square. which one is closer to 1
                //float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                if (direction == 0)
                    value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                if (direction == 1)
                    value = -x;

                if (direction == 2)
                    value = y;

                if (direction == 3)
                    value = x;

                if (direction == 4)
                    value = -y;

                map[i, j] = Interpolate(value, intensity);
            }
        }

        return map;
    }

    //modulates the values so its not linear but a graph
    static float Interpolate(float value, float moda)
    {
        // x^a / ( x^a + (b-bx)^a )
        return Mathf.Clamp01(Mathf.Pow(value, moda / (Mathf.Pow(value, moda) + Mathf.Pow(1 -value, moda))));
    }
}