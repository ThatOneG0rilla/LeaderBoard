using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Photon.VR.Player;
using TMPro;
using Photon.Voice.PUN;
using ExitGames.Client.Photon;
using System;
using System.Collections;
[Serializable]
public class LeaderBoardEntry
{
    public TextMeshPro UserNameText;
    public Renderer ColourSpot;
    public string PlayfabID;
    public GameObject ConfirmPanel;
    public GameObject ReportButton;
    public GameObject ReasonMenu;
    [HideInInspector] public string ReportReason;
}
public class LeaderBoardManager : MonoBehaviourPunCallbacks
{
    public LeaderBoardEntry[] Entrys;
    private PhotonVRPlayer[] PlayerCache;
    private LeaderBoardEntry LocalPlayer;
    private string LocalPlayFabID;
    private void Awake()
    {
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        },
        Result => {
            LocalPlayFabID = Result.PlayFabId;
            ExitGames.Client.Photon.Hashtable Props = new ExitGames.Client.Photon.Hashtable();
            Props["PlayFabPlayerID"] = LocalPlayFabID;
            PhotonNetwork.LocalPlayer.SetCustomProperties(Props);

            if (!PlayerPrefs.HasKey("PlayFabPlayerID"))
                PlayerPrefs.SetString("PlayFabPlayerID", LocalPlayFabID);
        },
        Error => Debug.LogError("PlayFab Login Failed: " + Error.GenerateErrorReport()));
    }
    public override void OnPlayerEnteredRoom(Player NewPlayer)
    {
        base.OnPlayerEnteredRoom(NewPlayer);
        StartCoroutine(RefreshPlayers());
    }
    public override void OnPlayerLeftRoom(Player OtherPlayer)
    {
        base.OnPlayerLeftRoom(OtherPlayer);
        StartCoroutine(RefreshPlayers());
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        StartCoroutine(RefreshPlayers());
    }
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        StartCoroutine(RefreshPlayers());
    }
    private IEnumerator RefreshPlayers()
    {
        yield return new WaitForSeconds(0.5f);
        PlayerCache = FindObjectsOfType<PhotonVRPlayer>();
    }
    private void Update()
    {
        for (int I = 0; I < Entrys.Length; I++)
        {
            if (I < PhotonNetwork.PlayerList.Length)
            {
                var PhotonPlayer = PhotonNetwork.PlayerList[I];

                Entrys[I].UserNameText.text = PhotonPlayer.NickName;
                Entrys[I].PlayfabID = PhotonPlayer.CustomProperties.TryGetValue("PlayFabPlayerID", out var Id) ? Id as string : null;
                Entrys[I].ColourSpot.material.color = (PhotonPlayer != null && PhotonPlayer.CustomProperties.TryGetValue("Colour", out object ColourObj) && ColourObj is string ColourString) ? JsonUtility.FromJson<Color>(ColourString) : Color.white;

                if (PhotonPlayer == PhotonNetwork.LocalPlayer)
                    LocalPlayer = Entrys[I];
            }
            else
            {
                Entrys[I].UserNameText.text = "";
                Entrys[I].ColourSpot.material.color = Color.white;
                Entrys[I].PlayfabID = null;
            }
        }
    }
    public void OnMuteClicked(int Index)
    {
        foreach (PhotonVRPlayer Player in PlayerCache)
            if (Player.photonView.Owner == PhotonNetwork.PlayerList[Index])
            {
                AudioSource AudioSource = Player.GetComponent<PhotonVoiceView>().SpeakerInUse.GetComponent<AudioSource>();
                AudioSource.mute = !AudioSource.mute;
                break;
            }
    }
    public void OnReportClicked(int Index)
    {
        if (Index >= 0 && Index < Entrys.Length)
        {
            Entrys[Index].ConfirmPanel.SetActive(false);
            Entrys[Index].ReasonMenu.SetActive(true);
            Entrys[Index].ReportButton.SetActive(false);
        }
    }
    public void OnConfirmReportClicked(int Index, string Reason)
    {
        if (Index >= 0 && Index < Entrys.Length)
        {
            Entrys[Index].ReportReason = Reason;
            Entrys[Index].ConfirmPanel.SetActive(true);
            Entrys[Index].ReasonMenu.SetActive(false);
            Entrys[Index].ReportButton.SetActive(false);
        }
    }
    public void CancelReport(int Index)
    {
        if (Index >= 0 && Index < Entrys.Length)
        {
            Entrys[Index].ConfirmPanel.SetActive(false);
            Entrys[Index].ReasonMenu.SetActive(false);
            Entrys[Index].ReportButton.SetActive(true);
        }
    }
    public void ConfirmReport(int Index)
    {
        if (Index < 0 || Index >= Entrys.Length || Entrys[Index] == null)
            return;
        var Reported = Entrys[Index];

        string LocalUserText = LocalPlayer.UserNameText.text ?? "NoUsernameFound";
        string OtherUserText = Reported.UserNameText.text ?? "NoUsernameFound";
        string ReporterColour = ColorUtility.ToHtmlStringRGBA(LocalPlayer.ColourSpot.material.color).Substring(0, 6);
        string TargetColour = ColorUtility.ToHtmlStringRGBA(Reported.ColourSpot.material.color).Substring(0, 6);

        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest
        {
            FunctionName = "ReportPlayer",
            FunctionParameter = new
            {
                ReporterId = LocalPlayer.PlayfabID,
                ReporterName = LocalUserText,
                ReporterColor = ReporterColour,
                TargetId = Reported.PlayfabID,
                TargetName = OtherUserText,
                TargetColor = TargetColour,
                Room = PhotonNetwork.CurrentRoom.Name,
                Reason = Reported.ReportReason,
            },
        },
        result => Debug.Log("PlayFab Result: " + result.FunctionResult),
        error => Debug.LogError("PlayFab Error: " + error.GenerateErrorReport()));

        Reported.ConfirmPanel.SetActive(false);
        Reported.ReasonMenu.SetActive(false);
        Reported.ReportButton.SetActive(true);
    }
}