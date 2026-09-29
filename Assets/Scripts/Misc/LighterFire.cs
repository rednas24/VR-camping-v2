using UnityEngine;

public class LighterFire : MonoBehaviour
{
    public GameObject lighterFlame;

    public void TurnFlameOn()
    {
        lighterFlame.SetActive(true);
    }

    public void TurnFlameOff()
    {
        lighterFlame.SetActive(false);
    }
}