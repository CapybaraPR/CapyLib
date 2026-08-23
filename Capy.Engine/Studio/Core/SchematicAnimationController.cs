using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Контроллер анимаций схематик в CapyStudio (для подвижных конструкций, лифтов, дверей и ловушек).
/// </summary>
public sealed class SchematicAnimationController
{
    private static readonly Dictionary<SchematicObject, SchematicAnimationController> Registry = new();

    public SchematicObject Schematic { get; }
    public IReadOnlyList<Animator> Animators { get; }

    public SchematicAnimationController(SchematicObject schematic)
    {
        Schematic = schematic;
        var list = new List<Animator>();

        if (schematic.RootObject != null)
        {
            list.AddRange(schematic.RootObject.GetComponentsInChildren<Animator>(true));
        }

        Animators = list;
        Registry[schematic] = this;
    }

    public static SchematicAnimationController Get(SchematicObject schematic)
    {
        if (Registry.TryGetValue(schematic, out var controller))
            return controller;

        return new SchematicAnimationController(schematic);
    }

    /// <summary>
    /// Убирает схематику из реестра (вызывается при Destroy, чтобы не копить мёртвые ссылки).
    /// </summary>
    public static void Unregister(SchematicObject schematic)
    {
        Registry.Remove(schematic);
    }

    public void Play(string stateName, int animatorIndex = 0)
    {
        if (animatorIndex >= 0 && animatorIndex < Animators.Count)
            Animators[animatorIndex].Play(stateName);
    }

    public void SetBool(string paramName, bool value, int animatorIndex = 0)
    {
        if (animatorIndex >= 0 && animatorIndex < Animators.Count)
            Animators[animatorIndex].SetBool(paramName, value);
    }

    public void Stop(int animatorIndex = 0)
    {
        if (animatorIndex >= 0 && animatorIndex < Animators.Count)
            Animators[animatorIndex].StopPlayback();
    }
}
