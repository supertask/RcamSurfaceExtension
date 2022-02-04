using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class HoloAsset : PlayableAsset
{
    public ExposedReference<GameObject> surface;
    
    //Fade in & Fade out
    public float deformInit; //30.0f
    public float deformDelta; //30.0f
    public float noiseDetailsInit; //7.5f
    public float fadeHeightInit; //0.0f
    public float fadeSpeed; //0.01f
    public bool isFadedIn;

    //Hololines
    public float holoLinesScrollingSpeed0;
    public float holoLinesScrollingSpeed1;
    public float holoLinesScrollingSpeed2;
    public float holoLinesScrollingSpeed3;

    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        HoloBehaviour behaviour = new HoloBehaviour();
        behaviour.surface = this.surface.Resolve(graph.GetResolver());
        behaviour.deformInit = this.deformInit;
        behaviour.deformDelta = this.deformDelta;
        behaviour.noiseDetailsInit = this.noiseDetailsInit;
        behaviour.fadeHeightInit = this.fadeHeightInit;
        behaviour.fadeSpeed = this.fadeSpeed;
        behaviour.isFadedIn = this.isFadedIn;
        behaviour.holoLinesScrollingSpeed0 = this.holoLinesScrollingSpeed0;
        behaviour.holoLinesScrollingSpeed1 = this.holoLinesScrollingSpeed1;
        behaviour.holoLinesScrollingSpeed2 = this.holoLinesScrollingSpeed2;
        return ScriptPlayable<HoloBehaviour>.Create(graph, behaviour);
    }
}
