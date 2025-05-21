using TcgEngine;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class CreatureData
{
    public int id;
    public string name;
    public string team;
    public int hp;
    public int damage;
    public string trait;
    public string image_card;
    public string image_board;
}

public class CardDataGenerator
{
    [MenuItem("Tools/Generar cartas desde JSON")]
    public static void GenerateCards()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Cards/WildTactics_creatures");
        if (jsonAsset == null)
        {
            Debug.LogError("No se encuentra el archivo JSON en Resources/Cards/WildTactics_creatures");
            return;
        }

        string json = jsonAsset.text;
        List<CreatureData> creatures = JsonUtility.FromJson<Wrapper>(WrapArray(json)).creatures;

        string savePath = "Assets/Resources/Cards/WildTactics/";
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        foreach (CreatureData creature in creatures)
        {
            CardData asset = ScriptableObject.CreateInstance<CardData>();
            asset.id = creature.id.ToString();
            asset.title = creature.name;
            asset.hp = creature.hp;
            asset.attack = creature.damage;

            asset.art_full = Resources.Load<Sprite>("Sprites/Cards/" + Path.GetFileNameWithoutExtension(creature.image_card));
            asset.art_board = Resources.Load<Sprite>("Sprites/CardsBoard/" + Path.GetFileNameWithoutExtension(creature.image_board));

            asset.team = Resources.Load<TeamData>("Teams/" + creature.team.ToLower());

            AssetDatabase.CreateAsset(asset, savePath + creature.id + "_" + creature.name + ".asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cartas creadas correctamente.");
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<CreatureData> creatures;
    }

    private static string WrapArray(string rawJson)
    {
        return "{\"creatures\":" + rawJson + "}";
    }
}
