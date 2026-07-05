using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 音频管理器：用有限通道（AudioSource 列表）统一管理音乐与音效，防止叠加爆音。
/// 通道 0 = BGM，通道 1/2 = 音效。后播的会覆盖同通道正在播放的音效。
/// 调用：AudioManager.Instance.Play(1, "CardDraw", false);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("===== BGM =====")]
    public AudioClip BGMMap;              // MAP01 地图背景循环

    [Header("===== 地图界面音效 (MAP) =====")]
    public AudioClip MapLevelClick;       // MAP02
    public AudioClip MapLevelLocked;      // MAP03
    public AudioClip ButtonHover;        // MAP04
    public AudioClip ButtonClick;         // MAP05
    public AudioClip ChapterTransition;  // MAP06

    [Header("===== 对局开局 & 基础操作 (BATT) =====")]
    public AudioClip Shuffle;             // BATT01
    public AudioClip CardDraw;            // BATT02 发牌
    public AudioClip DeclarationView;    // BATT03 查看AI宣言
    public AudioClip CardPlace;           // BATT04 放置卡牌

    [Header("===== 卡牌牌型 & 克制判定 (CARD) =====")]
    public AudioClip CardSingle;          // CARD01
    public AudioClip CardPair;            // CARD02
    public AudioClip CardStraight;        // CARD03
    public AudioClip CardThreeKind;       // CARD04
    public AudioClip CardFlush;           // CARD05
    public AudioClip CounterWin;          // CARD06
    public AudioClip CounterLose;         // CARD07
    public AudioClip RoundDraw;           // CARD08
    public AudioClip SuitEffect;          // CARD09

    [Header("===== 玩家千术 (SKILL) =====")]
    public AudioClip CheatSkill;          // SKILL01

    [Header("===== 结算 & 剧情 (END) =====")]
    public AudioClip RoundWin;            // END01
    public AudioClip RoundLose;           // END02
    public AudioClip DeckEmpty;           // END03

    [Header("===== 弹窗 & 通用 (SYS) =====")]
    public AudioClip PanelOpen;           // SYS01
    public AudioClip PanelClose;          // SYS02
    public AudioClip ValueChange;         // SYS03

    /// <summary>
    /// 音频播放通道列表：0 = BGM，1 / 2 = 音效
    /// </summary>
    private List<AudioSource> audios = new List<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < 3; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            audios.Add(source);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后自动恢复 BGM（通道 0 循环播放地图BGM）
        if (BGMMap != null && audios.Count > 0 && audios[0] != null)
        {
            if (!audios[0].isPlaying || audios[0].clip != BGMMap)
            {
                Play(0, "BGMMap", true);
            }
        }
    }

    private void Start()
    {
        Play(0, "BGMMap", true);
    }

    /// <summary>
    /// 统一播放接口
    /// </summary>
    public void Play(int index, string name, bool isLoop)
    {
        if (this == null) return;

        AudioClip clip = GetAudioClip(name);

        if (clip != null && index >= 0 && index < audios.Count && audios[index] != null)
        {
            AudioSource currentSource = audios[index];
            currentSource.clip = clip;
            currentSource.loop = isLoop;
            currentSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未配置或通道越界: {name} / index={index}");
        }
    }

    /// <summary>
    /// 停止指定通道
    /// </summary>
    public void Stop(int index)
    {
        if (this == null) return;
        if (index >= 0 && index < audios.Count && audios[index] != null)
            audios[index].Stop();
    }

    private AudioClip GetAudioClip(string name)
    {
        switch (name)
        {
            case "BGMMap": return BGMMap;
            case "MapLevelClick": return MapLevelClick;
            case "MapLevelLocked": return MapLevelLocked;
            case "ButtonHover": return ButtonHover;
            case "ButtonClick": return ButtonClick;
            case "ChapterTransition": return ChapterTransition;
            case "Shuffle": return Shuffle;
            case "CardDraw": return CardDraw;
            case "DeclarationView": return DeclarationView;
            case "CardPlace": return CardPlace;
            case "CardSingle": return CardSingle;
            case "CardPair": return CardPair;
            case "CardStraight": return CardStraight;
            case "CardThreeKind": return CardThreeKind;
            case "CardFlush": return CardFlush;
            case "CounterWin": return CounterWin;
            case "CounterLose": return CounterLose;
            case "RoundDraw": return RoundDraw;
            case "SuitEffect": return SuitEffect;
            case "CheatSkill": return CheatSkill;
            case "RoundWin": return RoundWin;
            case "RoundLose": return RoundLose;
            case "DeckEmpty": return DeckEmpty;
            case "PanelOpen": return PanelOpen;
            case "PanelClose": return PanelClose;
            case "ValueChange": return ValueChange;
            default: return null;
        }
    }
}
