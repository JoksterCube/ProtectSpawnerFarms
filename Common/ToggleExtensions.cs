﻿using BepInEx.Configuration;
using System;

namespace JoksterCube.ProtectSpawnerFarms.Common;

public static class ToggleExtensions
{
    public static Toggle Not(this Toggle toggle) => toggle switch
    {
        Toggle.On => Toggle.Off,
        Toggle.Off => Toggle.On,
        _ => throw new NotImplementedException()
    };

    public static bool IsOn(this Toggle toggle) => toggle == Toggle.On;

    public static bool IsOn(this ConfigEntry<Toggle> toggleConfig) => toggleConfig.Value.IsOn();

    public static Toggle ToToggle(this bool value) => value ? Toggle.On : Toggle.Off;
}
