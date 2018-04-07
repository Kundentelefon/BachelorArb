using UnityEngine;
using System.Collections;

//MonoBehaviour not needed because it is not applied to any Object. static because no multiple instances 
public static class Noise
{
    //so normalize dont make seams between chunks
    //local min,max
    //estimating global min, max
    public enum NormalizeMode { Local, Global};

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);  //pseudorandomnumber for seed
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        //sets the random number with a seed
        //at the end of this loop we have found the maxPossibleHeight value
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y; //minus so y offset gets inverted, scroll up to see whats below

            //makes it possible to scroll in x and y
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f; //clamp to min value 
        }

        //keep track for max height for normalizing and keeping numbers between 0 and 1
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        //zooming in in the middle instead of corner
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        //basic changes for perlin noise map
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1; //reset the values to 1
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    //at which point sampling the height values
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;  //scale of noise for noninteger values. offset is inside the brackets, so landmasses dont change in shape, while adjusting offset)
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // for mor interesting results * 2 -1 to get between -1 to 1; so sometimes noise is negative
                    noiseHeight += perlinValue * amplitude; //calculates height for each of the octaves. maximum Scenario is when perlinvalue is 1 every single octave  ->

                    amplitude *= persistance; //and amplitude also gets multiplied by persistence
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight; //apply to noise map
            }
        }

        //normalize noise hight to be between 0 and 1
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local) //local mode
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]); //min and max noise height will have slightly different values between the chunks, thats why its not perfectly lining up. if we generate the entire map at ones it wouldnt be a problem, because we know the max noise height, but endless terrain makes it a problem when its creates chunk by chunk
                }
                //do something else, that is consistent over the entire map
                else //global mode
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);  //reverse the operation where perlinValue is *2 and then subtracted with -1 above with adding +1 and dividing with maxPossibleHeight
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue); //Clamp so it is at least not less than 0
                }
            }
        }

        return noiseMap;
    }

}
