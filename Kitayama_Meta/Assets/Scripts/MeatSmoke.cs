using Kitayama.Cooking;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class MeatSmoke : MonoBehaviour
{
    public MeatCooking cooking;
    public CookingResult result;
    public Material smokeMaterial;

    [Range(1f, 12f)]
    public float particlesPerSecond = 5f;

    [Range(0.02f, 0.3f)]
    public float opacity = 0.13f;

    ParticleSystem smoke;
    Collider meatCollider;
    float emissionTime;

    void Awake()
    {
        if (cooking == null || smokeMaterial == null)
        {
            Debug.LogError(
                "Kitayama → Setup Cooking Smokeを実行してください。",
                this
            );

            enabled = false;
            return;
        }

        meatCollider = cooking.GetComponent<Collider>();

        GameObject child =
            new GameObject("SmallCookingSmoke");

        child.transform.SetParent(transform, false);

        smoke = child.AddComponent<ParticleSystem>();

        smoke.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        var main = smoke.main;
        main.playOnAwake = false;
        main.loop = true;

        // 出た煙はその場に残り、肉を投げても追いかけない。
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.scalingMode =
            ParticleSystemScalingMode.Shape;

        main.maxParticles = 16;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        // 加熱状態を確認して、コードから少量ずつ出す。
        var emission = smoke.emission;
        emission.enabled = false;

        var shape = smoke.shape;
        shape.enabled = false;

        // 現れてから、ゆっくり透明になる。
        var fade = smoke.colorOverLifetime;
        fade.enabled = true;

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        fade.color = gradient;

        // 上昇しながら少しだけ広がる。
        var size = smoke.sizeOverLifetime;
        size.enabled = true;

        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.Linear(
                0f, 0.6f,
                1f, 1.8f
            )
        );

        var renderer =
            smoke.GetComponent<ParticleSystemRenderer>();

        renderer.sharedMaterial = smokeMaterial;

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
    }

    void Update()
    {
        if (smoke == null)
            return;

        if (result != null && result.IsFinished)
        {
            ClearSmoke();
            return;
        }

        bool heating =
            cooking.isActiveAndEnabled &&
            cooking.Session != null &&
            cooking.Session.CurrentPhase ==
                CookingSession.Phase.Heating;

        if (!heating)
        {
            // 新しい煙だけ止める。
            // すでに出た煙は寿命まで残って自然に消える。
            emissionTime = 0f;
            return;
        }

        emissionTime += Time.deltaTime;

        float interval =
            1f / Mathf.Max(1f, particlesPerSecond);

        if (emissionTime < interval)
            return;

        // 処理が遅れたフレームでも一度に大量発生させない。
        emissionTime %= interval;

        Vector3 origin =
            cooking.transform.position +
            Vector3.up * 0.03f;

        if (meatCollider != null)
        {
            Bounds bounds = meatCollider.bounds;

            origin = new Vector3(
                bounds.center.x,
                bounds.max.y + 0.008f,
                bounds.center.z
            );
        }

        Vector2 offset =
            Random.insideUnitCircle * 0.025f;

        bool burnt =
            cooking.Session.Grade ==
            CookingSession.Doneness.Burnt;

        // 焦げても煙の量は増やさず、色だけ少し変える。
        float shade = burnt ? 0.52f : 0.9f;

        var particle = new ParticleSystem.EmitParams
        {
            position = origin + new Vector3(
                offset.x,
                0f,
                offset.y
            ),

            velocity = new Vector3(
                Random.Range(-0.015f, 0.015f),
                Random.Range(0.12f, 0.18f),
                Random.Range(-0.015f, 0.015f)
            ),

            startLifetime = Random.Range(0.65f, 1f),

            startSize = Random.Range(0.025f, 0.04f),

            startColor = new Color(
                shade,
                shade,
                shade,
                opacity
            ),

            rotation = Random.Range(0f, 360f)
        };

        if (!smoke.isPlaying)
            smoke.Play();

        smoke.Emit(particle, 1);
    }

    void ClearSmoke()
    {
        emissionTime = 0f;

        if (smoke != null)
        {
            smoke.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    void OnDisable()
    {
        ClearSmoke();
    }
}