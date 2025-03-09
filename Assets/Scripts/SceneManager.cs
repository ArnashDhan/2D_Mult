using UnityEngine;

public class SceneManager : MonoBehaviour
{
      
        public void LoadMainMenu()
        {
            Debug.Log("ChangingSCene to MainMenu");

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void LoadClashRoom()
        {
            Debug.Log("ChangingSCene to ClashRoom");
            UnityEngine.SceneManagement.SceneManager.LoadScene("ClashRoom");
        }

        public void LoadGameLobby()
        {
            Debug.Log("ChangingSCene to GameLobby");
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameLobby");
        }

        public void LoadWaitingList()
        {
            Debug.Log("ChangingSCene to WaitingList");
            UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingList");
        }
        public void LoadShop()
        {
            Debug.Log("ChangingSCene to Shop");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Shop");
        }
}



