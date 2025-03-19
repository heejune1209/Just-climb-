using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public void SFX1(int sfxIndex)
    {
        AudioManager.instance.PlaySFX(sfxIndex);
    }
}
