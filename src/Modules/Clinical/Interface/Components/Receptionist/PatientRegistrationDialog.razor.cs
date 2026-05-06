using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Receptionist;

public partial class PatientRegistrationDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    private string? _familyName;
    private string? _givenName;
    private string? _middleName;
    private DateTime? _birthDate;
    private string? _gender;
    private string? _phone;
    private string? _email;
    private string? _addressLine;
    private string? _city;
    private string? _state;
    private string? _postalCode;
    private string? _country;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_familyName) || string.IsNullOrWhiteSpace(_givenName) || !_birthDate.HasValue)
        {
            _errorMessage = Localizer["RequiredFieldsMissing"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new RegisterPatientCommand(
                new PatientRegistrationPayload
                {
                    FamilyName = _familyName!,
                    GivenName = _givenName!,
                    MiddleName = _middleName,
                    BirthDate = DateOnly.FromDateTime(_birthDate!.Value),
                    Gender = _gender,
                    Phone = _phone,
                    Email = _email,
                    AddressLine = _addressLine,
                    City = _city,
                    State = _state,
                    PostalCode = _postalCode,
                    Country = _country
                },
                RequestContext.UserId ?? string.Empty,
                RequestContext.TenantId ?? string.Empty));

            if (result.IsSuccess)
            {
                MudDialog.Close(DialogResult.Ok(result.FhirRefsCreated.FirstOrDefault()));
            }
            else
            {
                _errorMessage = result.ErrorMessage ?? result.ValidationErrors.FirstOrDefault()?.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
