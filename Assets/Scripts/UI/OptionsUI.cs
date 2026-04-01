using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeValueText;
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
    [SerializeField] private Button closeButton;
    //引用audio mixer
    [SerializeField] private AudioMixer audioMixer;
    public static OptionsUI Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
        //监听音量滑动条的值改变事件
        musicVolumeSlider.onValueChanged.AddListener(MusicVolumeSlider_OnValueChanged);
        sfxVolumeSlider.onValueChanged.AddListener(SfxVolumeSlider_OnValueChanged);
        closeButton.onClick.AddListener(() => Hide());
    }
    public void Start()
    {
        // 1. 拿到原始数据
        float savedMusicVol = DataMgr.Instance.SettingsData.musicVolume;
        float savedSfxVol = DataMgr.Instance.SettingsData.sfxVolume;

        // 2. 只改变滑动条的位置，不触发保存事件
        musicVolumeSlider.SetValueWithoutNotify(savedMusicVol);
        sfxVolumeSlider.SetValueWithoutNotify(savedSfxVol);

        // 3. 将 slider 的值乘 100 并取整，更好看一些
        musicVolumeValueText.text = (savedMusicVol * 100).ToString("F0");
        sfxVolumeValueText.text = (savedSfxVol * 100).ToString("F0");

        // ⭐ 4. 修复核心：手动把读取到的音量，算成分贝，告诉底层的调音台
        audioMixer.SetFloat("Music", Mathf.Log10(savedMusicVol) * 20f);
        audioMixer.SetFloat("SFX", Mathf.Log10(savedSfxVol) * 20f);

        Hide();
    } 
    private void MusicVolumeSlider_OnValueChanged(float value)
    {
        // 使用对数公式计算分贝 (dB)，这才是人耳真实的听觉感受
        float volume_dB = Mathf.Log10(value) * 20f;
        // 告诉调音台：把名字叫 SFX_Volume 的旋钮，拧到算好的分贝数
        audioMixer.SetFloat("Music", volume_dB);
        //更新文本显示
        musicVolumeValueText.text = (value * 100).ToString("F0");
        //保存设置数据
        DataMgr.Instance.SettingsData.musicVolume = value;
        DataMgr.Instance.SaveSettingsData();
    }
    private void SfxVolumeSlider_OnValueChanged(float value)
    {
        float volume_dB = Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("SFX", volume_dB);
        sfxVolumeValueText.text = (value * 100).ToString("F0");
        DataMgr.Instance.SettingsData.sfxVolume = value;
        DataMgr.Instance.SaveSettingsData();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
}
