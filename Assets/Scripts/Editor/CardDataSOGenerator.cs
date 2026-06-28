#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class CardDataSOGenerator
{
    [MenuItem("Tools/Generate Card Data SOs")]
    public static void GenerateAllCards()
    {
        string imageFolder = "Assets/Art/卡牌/card";
        string outputFolder = "Assets/Data/CardData";

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        string[] pngFiles = Directory.GetFiles(imageFolder, "*.png", SearchOption.TopDirectoryOnly);

        int createdCount = 0;
        int skippedCount = 0;

        foreach (string pngPath in pngFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(pngPath);

            if (fileName.Contains("Card_Back"))
            {
                skippedCount++;
                continue;
            }

            string[] parts = fileName.Split('_');
            if (parts.Length < 3)
            {
                Debug.LogWarning($"跳过无法解析的文件名: {fileName}");
                skippedCount++;
                continue;
            }

            string suitStr = parts[1];
            string rankStr = parts[2];

            Suit suit;
            if (!TryParseSuit(suitStr, out suit))
            {
                Debug.LogWarning($"无法解析花色: {suitStr} (文件: {fileName})");
                skippedCount++;
                continue;
            }

            Rank rank;
            if (!TryParseRank(rankStr, out rank))
            {
                Debug.LogWarning($"无法解析点数: {rankStr} (文件: {fileName})");
                skippedCount++;
                continue;
            }

            string assetName = $"CardData_{suitStr}_{rankStr}";
            string assetPath = $"{outputFolder}/{assetName}.asset";

            CardDataSO existing = AssetDatabase.LoadAssetAtPath<CardDataSO>(assetPath);
            if (existing != null)
            {
                skippedCount++;
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                Debug.LogWarning($"无法加载Sprite: {pngPath}");
                skippedCount++;
                continue;
            }

            CardDataSO newCard = ScriptableObject.CreateInstance<CardDataSO>();
            newCard.id = createdCount + 1;
            newCard.cardName = GetCardName(suit, rank);
            newCard.suit = suit;
            newCard.rank = rank;
            newCard.cardSprite = sprite;

            AssetDatabase.CreateAsset(newCard, assetPath);
            createdCount++;

            Debug.Log($"创建卡牌SO: {assetName} (ID: {newCard.id})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"批量生成完成！创建: {createdCount} 个，跳过: {skippedCount} 个");
    }

    private static bool TryParseSuit(string suitStr, out Suit suit)
    {
        switch (suitStr.ToLower())
        {
            case "spade":
                suit = Suit.Spade;
                return true;
            case "heart":
                suit = Suit.Heart;
                return true;
            case "diamond":
                suit = Suit.Diamond;
                return true;
            case "club":
                suit = Suit.Club;
                return true;
            default:
                suit = Suit.Spade;
                return false;
        }
    }

    private static bool TryParseRank(string rankStr, out Rank rank)
    {
        int num;
        if (int.TryParse(rankStr, out num))
        {
            if (num >= 2 && num <= 10)
            {
                rank = (Rank)num;
                return true;
            }
        }

        switch (rankStr.ToUpper())
        {
            case "J":
                rank = Rank.Jack;
                return true;
            case "Q":
                rank = Rank.Queen;
                return true;
            case "K":
                rank = Rank.King;
                return true;
            case "A":
                rank = Rank.Ace;
                return true;
            default:
                rank = Rank.Two;
                return false;
        }
    }

    private static string GetCardName(Suit suit, Rank rank)
    {
        string suitName = "";
        switch (suit)
        {
            case Suit.Spade: suitName = "黑桃"; break;
            case Suit.Heart: suitName = "红桃"; break;
            case Suit.Diamond: suitName = "方块"; break;
            case Suit.Club: suitName = "梅花"; break;
        }

        string rankName = "";
        switch (rank)
        {
            case Rank.Two: rankName = "2"; break;
            case Rank.Three: rankName = "3"; break;
            case Rank.Four: rankName = "4"; break;
            case Rank.Five: rankName = "5"; break;
            case Rank.Six: rankName = "6"; break;
            case Rank.Seven: rankName = "7"; break;
            case Rank.Eight: rankName = "8"; break;
            case Rank.Nine: rankName = "9"; break;
            case Rank.Ten: rankName = "10"; break;
            case Rank.Jack: rankName = "J"; break;
            case Rank.Queen: rankName = "Q"; break;
            case Rank.King: rankName = "K"; break;
            case Rank.Ace: rankName = "A"; break;
        }

        return $"{suitName}{rankName}";
    }
}
#endif
