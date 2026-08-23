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

    public string ServerBrandHintText { get; set; } = "<b><size=25><color=#ffa94e>КАПИБАРА</color></size></b>";
    public string SpectatorBottomPanel { get; set; } = "<color=#b8b8b8>Наблюдаю за:</color> {spectating}\n\nИгроки: <color=#ffa94e>{playersCurrent}/{playersMax}</color> | Наблюдатели: <color=#ffa94e>{spectators}</color>\n\n{serverBrand}";
    public string SpectatorListHeader { get; set; } = "<size=28><color=#ffa94e>\U0001F465 зрители: ({count})</color></size>";
    public string SpectatorListNameFormat { get; set; } = "<size=28><color=#ffa94e>• {nickname}</color></size>";
    
    public float SpectatorBottomPanelX { get; set; } = 0f;
    public float SpectatorBottomPanelY { get; set; } = 950f;
    public float RoundTimePositionX { get; set; } = 0f;
    public float RoundTimePositionY { get; set; } = 10f;
    public string RoundTimeHintText { get; set; } = "<color={color}><size=16>{roundTime}</size></color>";
}
