using UnityEngine;
using UnityEngine.UI; // If using standard UI Text
using TMPro; // If using TextMeshPro

public class DisplayTeams : MonoBehaviour
{
    public TextMeshProUGUI team1Text;  // UI Text for Team 1
    public TextMeshProUGUI team2Text;  // UI Text for Team 2

    void Start()
    {
        // Assuming the GameData.Teams contains player assignments
        DisplayTeamMembers();
    }

    void DisplayTeamMembers()
    {
        // Ensure GameData.Teams is not null
        if (GameData.Teams != null)
        {
            string team1Members = "Team 1: ";
            string team2Members = "Team 2: ";

            // Loop through the teams and assign player names
            foreach (var player in GameData.Teams)
            {
                if (player.Value == 1)
                {
                    team1Members += player.Key + ", "; // player.Key is the player ID (or name if you store it)
                }
                else if (player.Value == 2)
                {
                    team2Members += player.Key + ", ";
                }
            }

            // Remove the last comma and space
            team1Members = team1Members.TrimEnd(',', ' ');
            team2Members = team2Members.TrimEnd(',', ' ');

            // Display on UI
            team1Text.text = team1Members;
            team2Text.text = team2Members;
        }
        else
        {
            Debug.LogWarning("GameData.Teams is null. Please ensure teams are assigned before transitioning to this scene.");
        }
    }
}