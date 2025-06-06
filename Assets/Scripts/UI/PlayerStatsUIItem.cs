using TMPro;
using UnityEngine;

public class PlayerStatsUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text killsText;

    public void SetPlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void SetKills(int kills)
    {
        killsText.text = kills.ToString();
    }
}

