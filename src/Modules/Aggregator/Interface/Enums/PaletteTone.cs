namespace Vitarish.Modules.Aggregator.Interface.Enums;

/// <summary>
/// Selects one tone from the active Vitarish palette.
/// <para>
/// <see cref="Auto"/> does not emit a manual tone and therefore allows CSS
/// defaults or automatic palette-card tone cycling to decide the button tone.
/// </para>
/// </summary>
public enum PaletteTone
{
    Auto = 0,
    Tone100 = 100,
    Tone300 = 300,
    Tone700 = 700,
    Tone800 = 800,
    Tone900 = 900,
    Tone950 = 950
}
