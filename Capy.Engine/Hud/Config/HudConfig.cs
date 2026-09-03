namespace Capy.Engine.Hud.Config;

public class HudConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowServerBrand { get; set; } = true;
    public bool ShowRoundTime { get; set; } = true;
    public bool ShowRespawnTimers { get; set; } = true;
    public bool ShowGeneratorStatus { get; set; } = true;
    public bool ShowWarheadStatus { get; set; } = true;
    public bool ShowSpectatorList { get; set; } = true;
    public bool ShowSpectatorHumeShield { get; set; } = true;
    public bool ShowSpectatorAmmo { get; set; } = true;
    public bool ShowSpectatorArmor { get; set; } = true;

    public string ServerBrandHintText { get; set; } = "<b><size=25><color=#ffa94e>\u041A\u0410\u041F\u0418\u0411\u0410\u0420\u0410</color></size></b>";
    public string SpectatorBottomPanel { get; set; } = "<color=#b8b8b8>\u041D\u0430\u0431\u043B\u044E\u0434\u0430\u044E \u0437\u0430:</color> {spectating}\n\n\u0418\u0433\u0440\u043E\u043A\u0438: <color=#ffa94e>{playersCurrent}/{playersMax}</color> | \u041D\u0430\u0431\u043B\u044E\u0434\u0430\u0442\u0435\u043B\u0438: <color=#ffa94e>{spectators}</color>\n\n{serverBrand}";
    public string SpectatorListHeader { get; set; } = "<size=28><color=#ffa94e>\U0001F465 \u0437\u0440\u0438\u0442\u0435\u043B\u0438: ({count})</color></size>";
    public string SpectatorListNameFormat { get; set; } = "<size=28><color=#ffa94e>\u2022 {nickname}</color></size>";
    
    public float SpectatorBottomPanelX { get; set; } = 0f;
    public float SpectatorBottomPanelY { get; set; } = 950f;
    public float RoundTimePositionX { get; set; } = 0f;
    public float RoundTimePositionY { get; set; } = 10f;
    public string RoundTimeHintText { get; set; } = "<color={color}><size=16>{roundTime}</size></color>";
}
