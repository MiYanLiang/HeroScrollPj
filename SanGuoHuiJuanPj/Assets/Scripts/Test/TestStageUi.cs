using System.Collections.Generic;
using System.Linq;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class TestStageUi : MonoBehaviour
{
    public int Id => Formation == null ? -1 : Formation.Id;
    public Button Button;
    public Text Text;
    public SimpleFormation Formation;


    public void Set(SimpleFormation formation)
    {
        Formation = formation;
        UpdateUi();
    }
    private void UpdateUi()
    {
        var text = string.Empty;
        gameObject.SetActive(Formation != null);
        if (Formation != null)
            text = $"{Formation.Id}.【{Formation.Name}】单位【{Formation.Formation.Count - 1}】";
        Text.text = text;
    }

    public class Card : IGameCard
    {
        public int CardId { get; set; }
        public int Level { get; set; }
        public int Chips { get; set; }
        public int Type { get; set; }
        public int Arouse { get; set; }
        public int Deputy1Id { get; set; } = -1;
        public int Deputy1Level { get; set; }
        public int Deputy2Id { get; set; } = -1;
        public int Deputy2Level { get; set; }
        public int Deputy3Id { get; set; } = -1;
        public int Deputy3Level { get; set; }
        public int Deputy4Id { get; set; } = -1;
        public int Deputy4Level { get; set; }

        public Card()
        {
        }

        public Card(IGameCard c)
        {
            CardId = c.CardId;
            Level = c.Level;
            Chips = c.Chips;
            Type = c.Type;
            Arouse = c.Arouse;
            Deputy1Id = c.Deputy1Id;
            Deputy1Level = c.Deputy1Level;
            Deputy2Id = c.Deputy2Id;
            Deputy2Level = c.Deputy2Level;
            Deputy3Id = c.Deputy3Id;
            Deputy3Level = c.Deputy3Level;
            Deputy4Id = c.Deputy4Id;
            Deputy4Level = c.Deputy4Level;
        }
    }
    public class SimpleFormation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Dictionary<int, Card> Formation { get; set; }

    }

}