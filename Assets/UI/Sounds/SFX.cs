using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public void SFX1(int sfxIndex)
    {
        Managers.Sound.PlaySFX(sfxIndex);
    }
}
