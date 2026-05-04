using Godot;
using GodotArray = Godot.Collections.Array;

namespace Maze.Views;

/// <summary>
/// Baut ein ArrayMesh fuer einen getexturierten Quader mit pro Seite eigenen UV-Koordinaten.
/// Alle benoetigten UV-Konventionen stehen in diesem Plan (Phase 19).
///
/// Jede Seite hat 4 Vertices in der Reihenfolge:
///   First (top-left), Second (top-right), Third (bottom-left), Fourth (bottom-right)
/// und 2 Dreiecke (0,1,2)+(1,3,2). Insgesamt: 24 Vertices, 36 Indices, 12 Triangles.
/// </summary>
public static class TexturedCuboid
{
    /// <summary>
    /// Beschreibt ein UV-Rechteck einer Seite in Pixel-Koordinaten der Atlas-Textur.
    /// Negative Width/Height kehrt die UV-Achse um (= horizontaler / vertikaler Flip),
    /// das wird fuer den Spiegel-Look des rechten Arms genutzt.
    /// </summary>
    public readonly record struct UvRect(int X, int Y, int Width, int Height);

    public readonly record struct FaceUvs(
        UvRect Front, UvRect Right, UvRect Rear, UvRect Left, UvRect Top, UvRect Bottom);

    private const float AtlasWidth = 64f;
    private const float AtlasHeight = 32f;

    public static ArrayMesh Build(float width, float height, float depth, FaceUvs uvs)
    {
        var verts = new Vector3[24];
        var norms = new Vector3[24];
        var uv = new Vector2[24];
        var idx = new int[36];

        // Front (+Z)
        SetFace(verts, norms, uv, 0, new Vector3(0, 0, 1),
            new Vector3(0, height, depth), new Vector3(width, height, depth),
            new Vector3(0, 0, depth),       new Vector3(width, 0, depth),
            uvs.Front);

        // Right (+X)
        SetFace(verts, norms, uv, 4, new Vector3(1, 0, 0),
            new Vector3(width, height, depth), new Vector3(width, height, 0),
            new Vector3(width, 0, depth),       new Vector3(width, 0, 0),
            uvs.Right);

        // Rear (-Z)
        SetFace(verts, norms, uv, 8, new Vector3(0, 0, -1),
            new Vector3(width, height, 0), new Vector3(0, height, 0),
            new Vector3(width, 0, 0),       new Vector3(0, 0, 0),
            uvs.Rear);

        // Left (-X)
        SetFace(verts, norms, uv, 12, new Vector3(-1, 0, 0),
            new Vector3(0, height, 0), new Vector3(0, height, depth),
            new Vector3(0, 0, 0),       new Vector3(0, 0, depth),
            uvs.Left);

        // Top (+Y)
        SetFace(verts, norms, uv, 16, new Vector3(0, 1, 0),
            new Vector3(0, height, 0),     new Vector3(width, height, 0),
            new Vector3(0, height, depth), new Vector3(width, height, depth),
            uvs.Top);

        // Bottom (-Y)
        SetFace(verts, norms, uv, 20, new Vector3(0, -1, 0),
            new Vector3(0, 0, depth),     new Vector3(width, 0, depth),
            new Vector3(0, 0, 0),         new Vector3(width, 0, 0),
            uvs.Bottom);

        // Indices: 6 Seiten * 6 Indices = 36
        int cur = 0;
        for (int i = 0; i < 24; i += 4)
        {
            idx[cur++] = 0 + i;
            idx[cur++] = 1 + i;
            idx[cur++] = 2 + i;
            idx[cur++] = 1 + i;
            idx[cur++] = 3 + i;
            idx[cur++] = 2 + i;
        }

        var arrays = new GodotArray();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = norms;
        arrays[(int)Mesh.ArrayType.TexUV]  = uv;
        arrays[(int)Mesh.ArrayType.Index]  = idx;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static void SetFace(Vector3[] verts, Vector3[] norms, Vector2[] uv, int offset,
        Vector3 normal, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, UvRect rect)
    {
        verts[offset + 0] = v0;
        verts[offset + 1] = v1;
        verts[offset + 2] = v2;
        verts[offset + 3] = v3;

        norms[offset + 0] = normal;
        norms[offset + 1] = normal;
        norms[offset + 2] = normal;
        norms[offset + 3] = normal;

        // Negative Breite/Hoehe in UvRect = Spiegel-Flag (siehe ArmRight).
        float u0 = rect.X / AtlasWidth;
        float v0u = rect.Y / AtlasHeight;
        float u1 = (rect.X + rect.Width) / AtlasWidth;
        float v1u = (rect.Y + rect.Height) / AtlasHeight;

        uv[offset + 0] = new Vector2(u0,  v0u);  // top-left
        uv[offset + 1] = new Vector2(u1,  v0u);  // top-right
        uv[offset + 2] = new Vector2(u0,  v1u);  // bottom-left
        uv[offset + 3] = new Vector2(u1,  v1u);  // bottom-right
    }
}
