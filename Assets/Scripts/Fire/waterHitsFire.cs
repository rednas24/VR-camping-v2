using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WaterHitsFire : MonoBehaviour
{
    [SerializeField] private FireIgnitionZone fire;

    private ParticleSystem water;
    private readonly List<ParticleSystem.Particle> enteredDrops = new List<ParticleSystem.Particle>();

    private void Awake()
    {
        water = GetComponent<ParticleSystem>();
    }

    private void OnParticleTrigger()
    {
        int drops = water.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enteredDrops);

        if (drops > 0 && fire != null)
            fire.AddWater(drops);
    }
}