using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundRefSO", menuName = "Scriptable Objects/SoundRefSO")]
public class SoundRefSO : ScriptableObject
{
    public List<AudioClip> chopSoundList;
    public List<AudioClip> deliveryFailSoundList;
    public List<AudioClip> deliverySuccessSoundList;
    public List<AudioClip> footStepSoundList;
    public List<AudioClip> objectDropSoundList;
    public List<AudioClip> objectPickupSoundList;
    public AudioClip panSizzleLoopSound;
    public List<AudioClip> trashSoundList;
    public List<AudioClip> warningSoundList;
}
