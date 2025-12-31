namespace Vitarish.Interfaces.Base.Common;

public interface IExpandableItem
{
    bool Expanded { get; set; }
    Task Toggle();
    ElementReference Element { get; set; }
}
public partial class ExpandableItem : ComponentBase, IExpandableItem
{
    [Inject] public IScrollManager ScrollManager { get; set; } = default!;
    [Inject] public IFocusService FocusService { get; set; } = default!;
    [Parameter] public Guid Id { get; set; } = Guid.NewGuid();
    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public bool NoIcon { get; set; }
    [Parameter] public string OpenIcon { get; set; } = "fa-thin fa-folder-open";
    [Parameter] public string CloseIcon { get; set; } = "fa-thin fa-folder";
    [Parameter] public string? HighlightString { get; set; }
    [Parameter] public int Count { get; set; }
    [Parameter] public bool Focusable { get; set; } = true;
    [Parameter] public Color Color { get; set; }
    [Parameter] public Size Size { get; set; } = Size.Large;
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment<IExpandableItem>? TitleContent { get; set; }
    [Parameter] public RenderFragment? StartContent { get; set; }
    [Parameter] public RenderFragment? InlineContent { get; set; }
    [Parameter] public RenderFragment? MenuItems { get; set; }
    [Parameter] public RenderFragment? LoadingContent { get; set; }
    [Parameter] public bool ShowMenu { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback<bool> ExpandedChanged { get; set; }
    [Parameter] public bool Expanded { get; set; }
    public ElementReference Element { get; set; }
    private bool firstRenderDone;
    private bool menuOpen;
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            firstRenderDone = true;
            StateHasChanged();
        }
    }
    public async Task Toggle()
    {
        if (OnClicked.HasDelegate)
        {
            await OnClicked.InvokeAsync(Id);
        }
        else
        {
            Expanded = !Expanded;
            if (Focusable)
            {
                FocusService.SetFocus(Element, Expanded);
            }
            if (ExpandedChanged.HasDelegate)
            {
                await ExpandedChanged.InvokeAsync(Expanded);
            }
            StateHasChanged();
        }
    }

    private async void OnAnimationEnd()
    {

    }
}
