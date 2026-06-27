namespace Vitarish.Modules.Base.Interface.Services;

[RegisterScoped]
internal class FocusService : IFocusService
{
    public event FocusActivation? FocusActivation;
    public void SetFocus(ElementReference element, bool active) => FocusActivation?.Invoke(element, active);
}

public interface IFocusService
{
    event FocusActivation? FocusActivation;
    void SetFocus(ElementReference element, bool active);
}
public delegate void FocusActivation(object? sender, bool active);