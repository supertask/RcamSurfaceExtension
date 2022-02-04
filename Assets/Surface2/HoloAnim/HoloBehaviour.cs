using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

using Rcam2;

// A behaviour that is attached to a playable
public class HoloBehaviour : PlayableBehaviour
{
    public GameObject surface;
    private RcamSurface2 script;

    //Fade in & Fade out
    public float deformInit; // small: 5.0f, big: 30.0f
    public float deformDelta; //-0.35
    public float noiseDetailsInit; // small: 5.0f, big: 7.5f
    public float fadeHeightInit; //0.0f
    public float fadeSpeed; //0.01f
    public bool isFadedIn;

    //Hololines
    public float holoLinesScrollingSpeed0;
    public float holoLinesScrollingSpeed1;
    public float holoLinesScrollingSpeed2;
    public float holoLinesScrollingSpeed3;

    private Color holoLinesColor3; //Color(0,0,0,0)

    // Timeline 全体の開始時に走らせる処理.
    public override void OnGraphStart(Playable playable)
    {
    }

    // Called when the owning graph stops playing
    public override void OnGraphStop(Playable playable)
    {
        
    }

    //クリップの最初
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        this.script = surface.GetComponent<RcamSurface2>();
        this.script.deform = this.deformInit;
        this.script.noiseDetails = this.noiseDetailsInit;
        this.script.fadeHeight = this.fadeHeightInit;
        this.script.isFadedIn = this.isFadedIn;

        this.holoLinesColor3 = this.script.holoLinesColor3;
        
    }

    // Called when the state of the playable is set to Paused
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        
    }

    // Called each frame while the state is set to Play
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        this.script.fadeHeight += fadeSpeed * Time.deltaTime;
        this.script.deform += this.deformDelta;

        this.script.holoLinesMapOffset0.y += holoLinesScrollingSpeed0 * Time.deltaTime;
        this.script.holoLinesMapOffset1.y += holoLinesScrollingSpeed1 * Time.deltaTime;
        this.script.holoLinesMapOffset2.y += holoLinesScrollingSpeed2 * Time.deltaTime;
        this.script.holoLinesMapOffset3.y += holoLinesScrollingSpeed3 * Time.deltaTime;

        this.script.holoLinesColor3 = Color.Lerp(Color.black, this.holoLinesColor3, Mathf.Sin(Time.time));
    }
}
