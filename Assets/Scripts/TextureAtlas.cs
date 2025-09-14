using UnityEngine;
using UnityEditor;

public class TextureAtlasCreator : EditorWindow
{
    [MenuItem("Tools/Texture Atlas Creator")]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasCreator>("Atlas Creator");
    }

    public Texture2D[] textures = new Texture2D[16]; // 4x4 atlas
    public int atlasSize = 1024;
    public int tilesPerRow = 4;

    void OnGUI()
    {
        GUILayout.Label("Texture Atlas Creator", EditorStyles.boldLabel);

        atlasSize = EditorGUILayout.IntField("Atlas Size", atlasSize);
        tilesPerRow = EditorGUILayout.IntField("Tiles Per Row", tilesPerRow);

        GUILayout.Space(10);
        GUILayout.Label("Drag textures here:", EditorStyles.boldLabel);

        int totalTiles = tilesPerRow * tilesPerRow;
        if (textures.Length != totalTiles)
        {
            System.Array.Resize(ref textures, totalTiles);
        }

        for (int i = 0; i < totalTiles; i++)
        {
            textures[i] = EditorGUILayout.ObjectField($"Texture {i}", textures[i], typeof(Texture2D), false) as Texture2D;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Atlas"))
        {
            CreateAtlas();
        }

        if (GUILayout.Button("Create Atlas Material"))
        {
            CreateAtlasMaterial();
        }
    }

    void CreateAtlas()
    {
        int tileSize = atlasSize / tilesPerRow;
        Texture2D atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);

        // Rellenar con color base
        Color[] fillColors = new Color[atlasSize * atlasSize];
        for (int i = 0; i < fillColors.Length; i++)
        {
            fillColors[i] = Color.white;
        }
        atlas.SetPixels(fillColors);

        // Colocar cada textura en su posición
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] != null)
            {
                int x = (i % tilesPerRow) * tileSize;
                int y = (i / tilesPerRow) * tileSize;

                // Redimensionar textura si es necesario
                Texture2D resized = ResizeTexture(textures[i], tileSize, tileSize);

                atlas.SetPixels(x, y, tileSize, tileSize, resized.GetPixels());
            }
        }

        atlas.Apply();

        // Guardar como asset
        byte[] pngData = atlas.EncodeToPNG();
        string path = "Assets/Textures/TextureAtlas.png";
        System.IO.File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        Debug.Log($"Texture Atlas creado en: {path}");
    }

    void CreateAtlasMaterial()
    {
        // Buscar el atlas creado
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/TextureAtlas.png");

        if (atlas == null)
        {
            Debug.LogError("Crea primero el atlas!");
            return;
        }

        // Crear material con el shader de atlas
        Material atlasMaterial = new Material(Shader.Find("Custom/AtlasShader"));
        atlasMaterial.SetTexture("_MainTex", atlas);
        atlasMaterial.SetVector("_TileCount", new Vector4(tilesPerRow, tilesPerRow, 0, 0));
        atlasMaterial.name = "AtlasMaterial";

        AssetDatabase.CreateAsset(atlasMaterial, "Assets/Materials/AtlasMaterial.mat");
        AssetDatabase.SaveAssets();

        Debug.Log("Material de Atlas creado!");
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D resized = new Texture2D(width, height);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return resized;
    }
}
