using UnityEngine;

public static class CraterRingGenerator
{

    //substracts from noise so landmass is fully sorrounded
    public static float[,] GenerateCraterRing(int chunkSize, float craterSize, float craterIntensity, float posX, float posY, float ellipseX, float ellipseY)
    {
        float[,] map = new float[chunkSize, chunkSize];

        //radius
        int centerX = chunkSize / 2;
        int centerY = chunkSize / 2;

        float distanceX;
        float distanceY;

        //float ellipseX, float ellipseY, bool weightenedAngle

        float distanceToCenter;
        float distanceToCenter2;


        //float distanceToCenter;
        //i and j is coordinate of a point inside the square map
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                //one of them mult with 20 for ellipse
                distanceX = ellipseX * (centerX - posX - i) * (centerX - posX - i);
                distanceY = ellipseY * (centerY - posY - j) * (centerY - posY - j);

                distanceX /= Mathf.Pow((float)chunkSize * 2, 2);
                distanceY /= Mathf.Pow((float)chunkSize * 2, 2);

                // multiplicate for line graph 
                distanceToCenter = Mathf.Sqrt(distanceX + distanceY);

                //number shows how big the crater will be
                distanceToCenter2 = distanceToCenter / Mathf.Abs(craterSize);

                map[i, j] = IntensityOfCrater(distanceToCenter2, craterIntensity);
            }
        }

        return map;
    }

    static float IntensityOfCrater(float value, float intensity)
    {
        return Mathf.Clamp01((Mathf.Pow(value, intensity) * value) / value);
    }

    static float Fourir(float bob, float a, float b)
    {
        float value = 0;
        for (int i = 0; i < a; i++)
        {
            value = (float)((Mathf.PI / 10) - Mathf.Sin(i) / i * (Mathf.Cos(i * bob) * b));
        }

        return value;
    }

    static float Wsin(float value, float intensity, float modb)
    {
        return -((Mathf.Sin(value) * intensity) / value);
    }
}
