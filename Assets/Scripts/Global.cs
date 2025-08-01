using UnityEngine;
using UnityEngine.Serialization;

public class Global : MonoBehaviour{
    [SerializeField] public float heightPow;
    [SerializeField] public float noiseScale;
    [SerializeField] public float noiseAmp;
    [SerializeField] public Material blockMaterial;
    [SerializeField] public Material roadMaterial;
    [SerializeField] public float roadWidth;
    [SerializeField] public float blockHeightRange;
    [SerializeField] public float powScale; 
    [SerializeField] public int blockSplitAmount;

    public static Global inst { get; private set; }

    private void Awake(){
        if (inst != null && inst != this) { 
            Destroy(this); 
        } else { 
            inst = this; 
        } 
    }
    
}
