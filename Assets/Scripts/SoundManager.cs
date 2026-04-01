using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Audio;
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundRefSO soundRefSO;
    //音频输出组
    [SerializeField] private AudioMixerGroup audioMixerGroup;
    public static SoundManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        //普通订阅事件：当订单被送达时，播放送达成功的音效；当订单错误时，播放送达失败的音效
        DeliveryManager.Instance.OnDeliverSuccess += DeliveryManager_OnDeliverSuccess;
        DeliveryManager.Instance.OnDeliverFail += DeliveryManager_OnDeliverFail;
        //以下为通过类名订阅事件，事件定义时加上了static，因为场景中可以有多个实例
        CuttingCounter.OnAnyCutting += CuttingCounter_OnAnyCutting;
        BaseCounter.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
        TrashCounter.OnAnyObjectThrownHere += TrashCounter_OnAnyObjectThrownHere;
        //以下为通过实例订阅事件，事件定义时没有加上static，因为场景中只有一个实例
        Player.Instance.OnPickup += Player_OnPickup;
    }

    private void DeliveryManager_OnDeliverSuccess(object sender, System.EventArgs e)
    {
        PlaySound(soundRefSO.deliverySuccessSoundList, DeliveryCounter.Instance.transform.position);
    }

    private void DeliveryManager_OnDeliverFail(object sender, System.EventArgs e)
    {
        PlaySound(soundRefSO.deliveryFailSoundList, DeliveryCounter.Instance.transform.position);
    }
    private void CuttingCounter_OnAnyCutting(object sender, System.EventArgs e)
    {
        //在 C# 的标准事件模式中，object sender 永远指向触发（发出）这个事件的对象。
        //object 是所有类型的基类，所以需要将 sender 转换为 CuttingCounter 类型才能用.transform.position
        CuttingCounter cuttingCounter = sender as CuttingCounter;
        PlaySound(soundRefSO.chopSoundList, cuttingCounter.transform.position);
    }
    private void BaseCounter_OnAnyObjectPlacedHere(object sender, System.EventArgs e)
    {
        BaseCounter baseCounter = sender as BaseCounter;
        PlaySound(soundRefSO.objectDropSoundList, baseCounter.transform.position);
    }
    private void TrashCounter_OnAnyObjectThrownHere(object sender, System.EventArgs e)
    {
        TrashCounter trashCounter = sender as TrashCounter;
        PlaySound(soundRefSO.trashSoundList, trashCounter.transform.position);
    }
    private void Player_OnPickup(object sender, System.EventArgs e)
    {
        PlaySound(soundRefSO.objectPickupSoundList, Player.Instance.transform.position);
    }
    //重载PlaySound方法，可以传入一个音效列表
    private void PlaySound(List<AudioClip> audioClipList, Vector3 position, float volume = 1f)
    {
        AudioClip audioClip = audioClipList[Random.Range(0, audioClipList.Count)];
        //再调用PlaySound方法，传入随机选择的音效
        PlaySound(audioClip, position, volume);
    }

    private void PlaySound(AudioClip audioClip,Vector3 position, float volume = 1f)
    {
        //短暂创建空物体，并添加AudioSource组件，关联音频组，播放音效，最后销毁物体
        GameObject audioObject = new GameObject("AudioSource");
        audioObject.transform.position = position;
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = audioMixerGroup;
        audioSource.PlayOneShot(audioClip, volume);
        Destroy(audioObject, audioClip.length);
    }
    //专门为播放步音效而创建的方法，PlayerSound脚本里就不用再传入音效了
    public void PlayFootStepSound(Vector3 position, float volume = 1f)
    {
        PlaySound(soundRefSO.footStepSoundList, position, volume);
    }
}
