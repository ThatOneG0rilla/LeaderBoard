using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
public enum ReportReason
{
    Mute,
    Report,
    SelectReason,
    Confirm,
    Cancel
}
public class LeaderboardButton : MonoBehaviourPunCallbacks
{
    public ReportReason Action;
    public string Reason;
    public int Index;
    public LeaderBoardManager Manager;

    private bool Toggled;
    private Renderer Renderer;
    private TextMeshPro DisplayButtonText;
    private void Awake()
    {
        Renderer = GetComponent<Renderer>();
        DisplayButtonText = GetComponentInChildren<TextMeshPro>();
    }
    public override void OnPlayerEnteredRoom(Player NewPlayer) => Refresh();
    public override void OnPlayerLeftRoom(Player OtherPlayer) => Refresh();
    public override void OnJoinedRoom() => Refresh();
    public override void OnLeftRoom() => Refresh();
    private void Refresh()
    {
        if (Index > 0 && Index <= PhotonNetwork.PlayerList.Length)
        {
            Player Player = PhotonNetwork.PlayerList[Index - 1];
            bool IsLocalPlayer = Player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
            SetActive(!IsLocalPlayer);
        }
    }
    private void SetActive(bool State)
    {
        if (Renderer != null)
            Renderer.enabled = State;

        if (DisplayButtonText != null)
            DisplayButtonText.enabled = State;
    }
    private void OnTriggerEnter(Collider Other)
    {
        if (!Renderer.enabled || Index > PhotonNetwork.PlayerList.Length || Index <= 0)
            return;

        switch (Action)
        {
            case ReportReason.Mute:
                Toggled = !Toggled;
                Renderer.material.color = Toggled ? Color.red : Color.white;
                Manager.OnMuteClicked(Index - 1);
                break;
            case ReportReason.Report:
                Manager.OnReportClicked(Index - 1);
                break;
            case ReportReason.Confirm:
                Manager.ConfirmReport(Index - 1);
                break;
            case ReportReason.Cancel:
                Manager.CancelReport(Index - 1);
                break;
            case ReportReason.SelectReason:
                Manager.OnConfirmReportClicked(Index - 1, Reason);
                break;
        }
    }
}