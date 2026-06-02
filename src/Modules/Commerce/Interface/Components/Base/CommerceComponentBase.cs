namespace Vitarish.Commerce.Interface.Components.Base;

public abstract class CommerceComponentBase : ComponentBase
{
    [Inject]
    protected IAccessControlledProductService ProductService { get; set; } = default!;

    [Inject]
    protected IAccessControlledOrderService OrderService { get; set; } = default!;

    [Inject]
    protected ICartService CartService { get; set; } = default!;
}
