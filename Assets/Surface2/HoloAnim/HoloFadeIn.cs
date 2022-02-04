using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Rcam2; //後で変える

public class HoloFadeIn: MonoBehaviour
{
    private RcamSurface2 script;

    public void Start()
    {
        this.script = this.GetComponent<RcamSurface2>();
    }

    public void fadeIn()
    {
        this.script.fadeHeight = 0.5f;
    }
}
