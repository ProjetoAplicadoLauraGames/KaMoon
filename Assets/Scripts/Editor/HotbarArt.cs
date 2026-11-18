#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Desenha os ícones e a moldura da hotbar como PNGs 32x32 e atribui os ícones
/// aos HotbarItem. Sem IA, sem dependências: só pixels escritos no Editor.
/// </summary>
public static class HotbarArt
{
    private const string Pasta = "Assets/Hotbar/Icons";
    private const int Tamanho = 32;
    private const int TamanhoSlot = 64;
    private static Sprite moldura;
    private static Sprite faixa;
    private static Sprite placa;
    private static Sprite vazio;
    private static Sprite item;
    private static Sprite selecionado;

    public static Sprite Frame() => Carregar("slot-frame", ref moldura);
    public static Sprite Aviso() => Carregar("warning-stripe", ref faixa);
    public static Sprite Placa() => Carregar("panel-plate", ref placa);
    public static Sprite SlotVazio() => Carregar("slot-empty", ref vazio);
    public static Sprite SlotItem() => Carregar("slot-item", ref item);
    public static Sprite SlotSelecionado() => Carregar("slot-selected", ref selecionado);

    private static Sprite Carregar(string nome, ref Sprite cache)
    {
        if (cache == null) cache = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/" + nome + ".png");
        return cache;
    }

    public static void CriarEAssociar()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Hotbar/Icons"));

        Criar("slot-frame", DesenharMoldura);
        Criar("battery", DesenharBateria64, 64);
        Criar("flashlight", DesenharLanterna64, 64);
        Criar("screwdriver", DesenharChaveFendas64, 64);
        Criar("warning-stripe", DesenharFaixaAviso);
        Criar("panel-plate", DesenharPlaca);
        CriarSlot("slot-empty", new Color(0.07f, 0.13f, 0.15f), new Color(0.04f, 0.09f, 0.10f), new Color(0.16f, 0.30f, 0.30f));
        CriarSlot("slot-item", new Color(0.12f, 0.22f, 0.23f), new Color(0.07f, 0.15f, 0.16f), new Color(0.30f, 0.55f, 0.52f));
        CriarSlot("slot-selected", new Color(0.72f, 0.46f, 0.18f), new Color(0.55f, 0.34f, 0.12f), new Color(0.90f, 0.60f, 0.25f));

        Atribuir("Battery", "battery");
        Atribuir("Flashlight", "flashlight");
        Atribuir("Screwdriver", "screwdriver");

        moldura = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/slot-frame.png");
        vazio = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/slot-empty.png");
        item = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/slot-item.png");
        selecionado = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/slot-selected.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Criar(string nome, Action<Color32[]> desenho, int tamanho = Tamanho)
    {
        string caminho = Pasta + "/" + nome + ".png";
        var pixels = new Color32[tamanho * tamanho];
        desenho(pixels);

        var textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        textura.SetPixels32(pixels);
        textura.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Hotbar/Icons/" + nome + ".png"), textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
        var importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = tamanho;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.filterMode = FilterMode.Bilinear;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.SaveAndReimport();
    }

    private static void CriarSlot(string nome, Color topo, Color baseCor, Color borda)
    {
        var pixels = new Color32[TamanhoSlot * TamanhoSlot];
        const float raio = 10f;
        for (int y = 0; y < TamanhoSlot; y++)
            for (int x = 0; x < TamanhoSlot; x++)
            {
                float cx = Mathf.Clamp(x, raio, TamanhoSlot - 1 - raio);
                float cy = Mathf.Clamp(y, raio, TamanhoSlot - 1 - raio);
                if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) > raio) continue;

                float t = (float)y / (TamanhoSlot - 1);
                Color cor = Color.Lerp(baseCor, topo, t);
                float distBorda = Mathf.Min(Mathf.Min(x, TamanhoSlot - 1 - x), Mathf.Min(y, TamanhoSlot - 1 - y));
                if (distBorda < 2 || Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) > raio - 2f)
                    cor = borda;
                pixels[y * TamanhoSlot + x] = cor;
            }

        string caminho = Pasta + "/" + nome + ".png";
        var textura = new Texture2D(TamanhoSlot, TamanhoSlot, TextureFormat.RGBA32, false);
        textura.SetPixels32(pixels);
        textura.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Hotbar/Icons/" + nome + ".png"), textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
        var importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = TamanhoSlot;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.filterMode = FilterMode.Bilinear;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.SaveAndReimport();
    }

    private static void Atribuir(string asset, string icone)
    {
        var item = AssetDatabase.LoadAssetAtPath<HotbarItem>("Assets/Hotbar/" + asset + ".asset");
        if (item == null) return;
        item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/" + icone + ".png");
        EditorUtility.SetDirty(item);
    }

    private static void DesenharBateria64(Color32[] p)
    {
        RetanguloAA(p, 16, 18, 48, 52, 3);
        RetanguloAA(p, 26, 12, 38, 18, 3);
        LinhaAA(p, 35, 24, 26, 36, 3);
        LinhaAA(p, 26, 36, 34, 36, 3);
        LinhaAA(p, 34, 36, 30, 47, 3);
        LinhaAA(p, 20, 23, 20, 47, 1);
    }

    private static void DesenharLanterna64(Color32[] p)
    {
        LinhaAA(p, 18, 14, 46, 14, 3);
        LinhaAA(p, 18, 14, 25, 32, 3);
        LinhaAA(p, 46, 14, 39, 32, 3);
        LinhaAA(p, 25, 32, 39, 32, 3);
        RetanguloAA(p, 28, 32, 36, 56, 3);
        LinhaAA(p, 28, 39, 36, 39, 3);
        LinhaAA(p, 14, 11, 10, 7, 2);
        LinhaAA(p, 50, 11, 54, 7, 2);
    }

    private static void DesenharChaveFendas64(Color32[] p)
    {
        RetanguloAA(p, 24, 10, 40, 34, 3);
        LinhaAA(p, 27, 16, 37, 16, 2);
        LinhaAA(p, 27, 22, 37, 22, 2);
        LinhaAA(p, 32, 34, 32, 56, 3);
        LinhaAA(p, 27, 56, 37, 56, 3);
    }

    private static void Marcar(Color32[] p, int x, int y, float alfa)
    {
        if (x < 0 || y < 0 || x >= 64 || y >= 64) return;
        byte a = (byte)(Mathf.Clamp01(alfa) * 255f);
        int i = y * 64 + x;
        if (a > p[i].a) p[i] = new Color32(255, 255, 255, a);
    }

    private static void LinhaAA(Color32[] p, float x0, float y0, float x1, float y1, float espessura)
    {
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Distancia(x, y, x0, y0, x1, y1);
                float a = Mathf.Clamp01(espessura * 0.5f + 0.5f - d);
                if (a > 0.02f) Marcar(p, x, y, a);
            }
    }

    private static void RetanguloAA(Color32[] p, int x0, int y0, int x1, int y1, float espessura)
    {
        LinhaAA(p, x0, y0, x1, y0, espessura);
        LinhaAA(p, x1, y0, x1, y1, espessura);
        LinhaAA(p, x1, y1, x0, y1, espessura);
        LinhaAA(p, x0, y1, x0, y0, espessura);
    }

    private static void DesenharMoldura(Color32[] p)
    {
        for (int y = 0; y < Tamanho; y++)
            for (int x = 0; x < Tamanho; x++)
            {
                bool borda = x < 2 || x > 29 || y < 2 || y > 29;
                float distanciaCanto = (31 - x) + y;
                if (x > 25 && y < 6 && (31 - x) + y < 6) borda = false;
                if (x > 25 && y < 6 && Mathf.Abs(distanciaCanto - 6) < 1.2f) borda = true;
                if (borda) p[y * Tamanho + x] = Branco();
            }
    }

    private static void DesenharFaixaAviso(Color32[] p)
    {
        for (int y = 0; y < Tamanho; y++)
            for (int x = 0; x < Tamanho; x++)
            {
                bool amarelo = ((x + y) / 6) % 2 == 0;
                p[y * Tamanho + x] = amarelo
                    ? new Color32(230, 153, 63, 255)
                    : new Color32(18, 14, 8, 255);
            }
    }

    private static void DesenharPlaca(Color32[] p)
    {
        for (int y = 0; y < Tamanho; y++)
            for (int x = 0; x < Tamanho; x++)
            {
                bool rebite = (x - 27) * (x - 27) + (y - 4) * (y - 4) < 4;
                p[y * Tamanho + x] = rebite
                    ? new Color32(120, 150, 148, 255)
                    : new Color32(10, 22, 25, 245);
            }
    }

    private static void DesenharBateria(Color32[] p)
    {
        Retangulo(p, 8, 10, 22, 26);
        Retangulo(p, 12, 7, 18, 10);
        Linha(p, 16, 14, 12, 20);
        Linha(p, 12, 20, 17, 20);
        Linha(p, 17, 20, 14, 25);
    }

    private static void DesenharLanterna(Color32[] p)
    {
        Linha(p, 11, 7, 21, 7);
        Linha(p, 11, 7, 13, 15);
        Linha(p, 21, 7, 19, 15);
        Linha(p, 13, 15, 19, 15);
        Retangulo(p, 14, 15, 18, 26);
        Linha(p, 9, 9, 7, 7);
        Linha(p, 23, 9, 25, 7);
    }

    private static void DesenharChaveFendas(Color32[] p)
    {
        Retangulo(p, 13, 6, 20, 16);
        Linha(p, 16, 16, 16, 26);
        Linha(p, 13, 26, 19, 26);
        Linha(p, 14, 9, 19, 9);
        Linha(p, 14, 12, 19, 12);
    }

    private static Color32 Branco() => new Color32(255, 255, 255, 255);

    private static void Linha(Color32[] p, float x0, float y0, float x1, float y1)
    {
        for (int y = 0; y < Tamanho; y++)
            for (int x = 0; x < Tamanho; x++)
                if (Distancia(x, y, x0, y0, x1, y1) <= 1.1f) p[y * Tamanho + x] = Branco();
    }

    private static void Retangulo(Color32[] p, int x0, int y0, int x1, int y1)
    {
        Linha(p, x0, y0, x1, y0);
        Linha(p, x1, y0, x1, y1);
        Linha(p, x1, y1, x0, y1);
        Linha(p, x0, y1, x0, y0);
    }

    private static float Distancia(float x, float y, float x0, float y0, float x1, float y1)
    {
        float dx = x1 - x0, dy = y1 - y0;
        float t = Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / Mathf.Max(0.0001f, dx * dx + dy * dy));
        return Vector2.Distance(new Vector2(x, y), new Vector2(x0 + t * dx, y0 + t * dy));
    }
}
#endif
