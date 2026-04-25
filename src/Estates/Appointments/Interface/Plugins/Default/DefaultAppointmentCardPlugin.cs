using Vitarish.Appointments.Interface.Plugins.Default.Components;

namespace Vitarish.Appointments.Interface.Plugins.Default;

internal class DefaultAppointmentCardPlugin : IAppointmentCardPlugin
{
    public string Id => PluginIdentifiers.Default;

    public RenderFragment Render(AppointmentCardPluginContext context) => builder =>
    {
        builder.OpenComponent<DefaultAppointmentCard>(0);
        builder.AddAttribute(1, nameof(context.Appointment), context.Appointment);
        builder.AddAttribute(2, nameof(context.OnEdit), EventCallback.Factory.Create(this, context.OnEdit));
        builder.AddAttribute(3, nameof(context.OnDeleted), EventCallback.Factory.Create(this, context.OnDeleted));
        builder.CloseComponent();
    };
}
