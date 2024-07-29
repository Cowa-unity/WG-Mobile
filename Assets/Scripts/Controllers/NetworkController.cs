using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkController : MonoBehaviourPunCallbacks
{
    private byte maxPlayersPerRoom = 2;
    private string gameVersion = "1.0.0";
    private bool waitingPlayer = false;
    private string scene = "Menu";
    private bool lookingForGame = false;
    public GameObject playerManagerPlayer1Prefab;
    public GameObject playerManagerPlayer2Prefab;
    public PhotonView playerManagerPlayer1; 
    public PhotonView playerManagerPlayer2;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game")
        {
            scene = "Game";
            if (PhotonNetwork.IsMasterClient)
            {
                GameObject playerManagerObj = PhotonNetwork.Instantiate(playerManagerPlayer1Prefab.name, new Vector3(220, 130f, 110f), Quaternion.Euler(55f, -90, 0f), 0);
                playerManagerPlayer1 = playerManagerObj.GetComponent<PhotonView>();
            }
            else
            {
                GameObject playerManagerObj = PhotonNetwork.Instantiate(playerManagerPlayer2Prefab.name, new Vector3(220, 130f, 110f), Quaternion.Euler(55f, -90, 0f), 0);
                playerManagerPlayer2 = playerManagerObj.GetComponent<PhotonView>();
            }
        }
    }

    private void Update()
    {
        if(scene == "Menu")
        {
            if(waitingPlayer)
            {
                if(PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("Players ready, opening game scene");
                    PhotonNetwork.LoadLevel("Game");
                    waitingPlayer = false;
                }
            }
        }
        else if(scene == "Game")
        {     
            if(PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                PhotonNetwork.LeaveRoom();
                PhotonNetwork.Disconnect();
                PhotonNetwork.LoadLevel("Menu");
                scene = "Menu";
            }
            if(playerManagerPlayer1 == null)
            {
                GameObject player1 = GameObject.FindGameObjectsWithTag("Player1").Length > 0 ? GameObject.FindGameObjectsWithTag("Player1")[0] : null;
                if(player1 != null) playerManagerPlayer1 = player1.GetComponent<PhotonView>();
            }
            if(playerManagerPlayer2 == null)
            {
                GameObject player2 = GameObject.FindGameObjectsWithTag("Player2").Length > 0 ? GameObject.FindGameObjectsWithTag("Player2")[0] : null;
                if(player2 != null) playerManagerPlayer2 = player2.GetComponent<PhotonView>();
            }
        }
    }

    public void Connect()
    {
        Debug.Log("Connexion");
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = gameVersion;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected");
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Looking for room");
    }

    public override void OnJoinedRoom()
    {
        waitingPlayer = true;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found, creating one");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    /*public void SetPseudo()
    {
        string playerPseudo = pseudoInput.text;
        PhotonNetwork.NickName = playerPseudo; 
    }*/
}