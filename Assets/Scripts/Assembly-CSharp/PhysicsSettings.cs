using System;
using UnityEngine;

public class PhysicsSettings : SettingsBase
{
    public float GravityX { get; set; }
    public float GravityY { get; set; }
    public bool NoDrag { get; set; }
    public float TimeScale { get; set; }

    public PhysicsSettings()
    {
        GravityX = Physics.gravity.x;
        GravityY = Physics.gravity.y;
        NoDrag = false;
        TimeScale = 1f;
    }

    public PhysicsSettings(PhysicsSettings settings)
    {
        GravityX = settings.GravityX;
        GravityY = settings.GravityY;
        NoDrag = settings.NoDrag;
        TimeScale = settings.TimeScale;
    }

    public override void Initialize()
    {
        Physics.gravity = new Vector2(GravityX, GravityY);
        ValueVariant value = new ValueVariant(NoDrag);
        INSettings.SetValue(INFeature.NoDrag, new Variant(value));
        value = new ValueVariant(TimeScale);
        INSettings.SetValue(INFeature.TimeScale, new Variant(value));
    }

    public void SetGravityX(float value)
    {
        GravityX = value;
        Physics.gravity = new Vector2(GravityX, GravityY);
    }

    public void SetGravityY(float value)
    {
        GravityY = value;
        Physics.gravity = new Vector2(GravityX, GravityY);
    }

    public void SetNoDrag(bool value)
    {
        NoDrag = value;
        ValueVariant value2 = new ValueVariant(NoDrag);
        INSettings.SetValue(INFeature.NoDrag, new Variant(value2));
    }

    public void SetTimeScale(float value)
    {
        TimeScale = UnityEngine.Mathf.Clamp(value, 0f, 10f);
        ValueVariant value2 = new ValueVariant(TimeScale);
        INSettings.SetValue(INFeature.TimeScale, new Variant(value2));
    }
}