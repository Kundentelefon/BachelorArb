using UnityEngine;
using System.Collections;

//for Plane Object
public class MapDisplay : MonoBehaviour
{

    public Renderer texturRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    //generate 1D Colormap from 2D Noisemap/texture
    public void DrawTexture(Texture2D texture)
    {
        texturRender.sharedMaterial.mainTexture = texture; //sharedmaterial: preview map inside editor without starting map
        texturRender.transform.localScale = new Vector3(texture.width, 1, texture.height);  //width and height of the texture
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
