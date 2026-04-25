using Vitarish.Appointments.Interface.Plugins.Default.Components;

namespace Vitarish.Appointments.Interface.Plugins.Default;

internal class DefaultAppointmentFormPlugin : IAppointmentFormPlugin
{
    public string Id => PluginIdentifiers.Default;

    public RenderFragment Render(AppointmentFormPluginContext context) => builder =>
    {
        builder.OpenComponent<DefaultAppointmentForm>(0);
        builder.AddAttribute(1, nameof(context.Appointment), context.Appointment);
        builder.AddAttribute(2, nameof(context.OnSaved), EventCallback.Factory.Create<Guid>(this, context.OnSaved));
        builder.AddAttribute(3, nameof(context.OnCancelled), EventCallback.Factory.Create(this, context.OnCancelled));
        builder.CloseComponent();
    };
}
