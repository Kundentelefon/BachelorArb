using UnityEngine;
using System.Collections;

public static class TextureGenerator
{

    //Create a Texture of 1D ColorMap
    public static Texture2D ColourMapTexture(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; //Point = takes bluriness for well defined blocks
        texture.wrapMode = TextureWrapMode.Clamp; //you cant no longer see the other side of the map on the edge
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    //Get Texture from 2D heightmap
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0); // 0 first Dimension
        int height = heightMap.GetLength(1); // 1 second Dimension

        Color[] colourMap = new Color[width * height]; //set colour of each pixel. its faster to first generate an array for all of the colors for all of the pixels and then set them all at ones
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //figure out index with y * width + x
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]); //Color between black and white
            }
        }

        return ColourMapTexture(colourMap, width, height);
    }

}
