using UnityEngine;
using System.Collections;

public static class FalloffGenerator
{

    //substracts from noise so landmass is fully sorrounded
    public static float[,] GenerateFalloffMap(int size)
    {
        //direction: 0 = top, 1 = right, 2 = bottom, 3 = left, 4 = all sides
        float[,] map = new float[size, size];

        float x;
        float y;
        float value = 0;
        int direction = 4; //space holder

        //i and j is coordinate of a point inside the square map
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                //take the coordinates and make them in a range from -1 to 1
                x = i / (float)size * 2 - 1;
                y = j / (float)size * 2 - 1;

                //get the value to use for map, find out which one, x or y, is closest to the edge of the square. which one is closer to 1
                //float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                if(direction == 0)
                value =  -y;

                if (direction == 1)
                    value = -x;

                if (direction == 2)
                    value = y;

                if (direction == 3)
                    value = x;

                if (direction == 4)
                    value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    //modulates the values so its not linear but a graph
    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;
        // x^a / ( x^a + (b-bx)^a )
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}