namespace Vitarish.Appointments.Contracts.Routes;

public static class AppointmentsApiRoutes
{
    public const string Base = "/api/modules/appointments";
    public const string Reservations = "/reservations";
    public const string ReservationById = Reservations + "/{id:guid}";
    public const string ReservationMove = ReservationById + "/move";
    public const string ReservationStatus = ReservationById + "/status";
    public const string Holds = "/holds";
    public const string HoldConfirm = Holds + "/confirm";
    public const string HoldRelease = Holds + "/release";
    public const string Shifts = "/shifts";
    public const string ShiftById = Shifts + "/{id:guid}";
    public const string ShiftStatus = ShiftById + "/status";
    public const string BookableDates = Shifts + "/bookable-dates";
    public const string BookableSlots = Shifts + "/bookable-slots";
    public const string Services = "/services";
    public const string ServiceById = Services + "/{id:guid}";
    public const string ServiceTypes = "/service-types";
    public const string ServiceTypeById = ServiceTypes + "/{id:guid}";
    public const string ServiceProviders = "/service-providers";
    public const string ServiceProviderById = ServiceProviders + "/{id:guid}";
    public const string LinkServiceProviderUser = ServiceProviderById + "/users";
    public const string UnlinkServiceProviderUser = ServiceProviderById + "/users/{userId}";
    public const string ReminderPolicies = "/reminder-policies";
    public const string ReminderPolicyById = ReminderPolicies + "/{id:guid}";
    public const string BookingPolicies = "/booking-policies";
    public const string BookingPolicyById = BookingPolicies + "/{id:guid}";
}
