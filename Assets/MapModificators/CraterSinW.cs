using UnityEngine;

public static class CraterSinW
{
    public static float[,] GenerateCraterSinW(int chunkSize, float intensity, float posX, float posY, float ellipseX, float ellipseY, float sinQuantity)
    {
        float[,] map = new float[chunkSize, chunkSize];

        //radius
        int centerX = chunkSize / 2;
        int centerY = chunkSize / 2;

        float distanceX;
        float distanceY;

        //float ellipseX, float ellipseY, bool weightenedAngle
        float distanceToCenter;

        //float distanceToCenter;
        //i and j is coordinate of a point inside the square map
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {

                //float x = i / (float)chunkSize * 2 - 1;
                //float y = j / (float)chunkSize * 2 - 1;

                //one of them mult with 20 for ellipse
                distanceX = ellipseX * (centerX - posX - i) * (centerX - posX - i);
                distanceY = ellipseY * (centerY - posY - j) * (centerY - posY - j);

                // multiplicate for line graph 
                distanceToCenter = Mathf.Sqrt(distanceX + distanceY);

                //number shows how big the crater will be
                //distanceToCenter2 = distanceToCenter / Mathf.Abs(intensity)/100;

                map[i, j] = Mathf.Abs(SinW(distanceToCenter, intensity, sinQuantity));
            }
        }

        return map;
    }

    static float SinW(float value, float intensity, float b)
    {
        return -(Mathf.Sin(value*b) * intensity / value);
    }
}
