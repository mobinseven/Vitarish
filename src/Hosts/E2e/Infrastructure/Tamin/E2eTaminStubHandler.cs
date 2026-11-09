using System.Net;
using System.Text;
using System.Text.Json;

namespace Vitarish.E2e.Infrastructure.Tamin;

/// <summary>
/// Stands in for the Tamin service at the outermost HTTP boundary, so the real adapter, SDK, token store and
/// guards run. It answers only the calls the drug-prescription scenario makes and fails every other request,
/// so no E2E run can reach the real service. It never calls an inner handler.
/// </summary>
internal sealed class E2eTaminStubHandler : DelegatingHandler
{
    public const string DrugCode = "E2E-DRUG-1";
    public const string DosageFormCode = "17";
    public const string AmountId = "580";
    public const string InstructionId = "2546009";
    public const string RouteCode = "231271000";
    public const long HeaderId = 3_000_000_001;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (request.Method == HttpMethod.Post && path.EndsWith("/auth/server/v2/token", StringComparison.Ordinal))
        {
            return Json("""{"access_token":"e2e-access-token","refresh_token":"e2e-refresh-token","expires_in":3600}""");
        }

        if (request.Method == HttpMethod.Get && path.EndsWith($"/ep/medication-instructions-by-dosage-form/v1/{DosageFormCode}", StringComparison.Ordinal))
        {
            return Json(MedicationInstructions);
        }

        if (request.Method == HttpMethod.Get && path.Contains("/interface/epresc/patient/v2/deserve-info/", StringComparison.Ordinal))
        {
            return Json("""{"status":"ELIGIBLE","trackingCode":"E2E-ENTITLEMENT"}""");
        }

        if (request.Method == HttpMethod.Post && path.EndsWith("/interface/epresc/SendEpresc/v2", StringComparison.Ordinal))
        {
            string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return AcceptsDrugPrescription(body)
                ? Json($$"""{"head_EPRSC_ID":{{HeaderId}},"trackingCode":987654}""")
                : Json("""{"error_Code":"E2E-400","error_Msg":"The drug prescription is incomplete."}""");
        }

        return new HttpResponseMessage(HttpStatusCode.NotImplemented)
        {
            Content = new StringContent($"The E2E Tamin stub does not answer {request.Method} {path}.", Encoding.UTF8)
        };
    }

    /// <summary>A drug prescription is accepted only with what the guide makes mandatory.</summary>
    private static bool AcceptsDrugPrescription(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("docMobileNo", out JsonElement mobile) || string.IsNullOrWhiteSpace(mobile.GetString()))
            {
                return false;
            }

            if (!root.TryGetProperty("noteDetailEprscs", out JsonElement items) || items.GetArrayLength() != 1)
            {
                return false;
            }

            JsonElement item = items[0];
            return item.TryGetProperty("srvId", out JsonElement service)
                && service.TryGetProperty("srvType", out JsonElement type)
                && type.TryGetProperty("srvType", out JsonElement typeCode)
                && typeCode.GetString() == "01"
                && item.TryGetProperty("timesAday", out JsonElement amount)
                && amount.TryGetProperty("drugAmntId", out JsonElement amountId)
                && amountId.GetInt32() == int.Parse(AmountId, System.Globalization.CultureInfo.InvariantCulture)
                && item.TryGetProperty("drugInstruction", out JsonElement instruction)
                && instruction.TryGetProperty("drugInstId", out JsonElement instructionId)
                && instructionId.GetInt32() == int.Parse(InstructionId, System.Globalization.CultureInfo.InvariantCulture)
                && item.TryGetProperty("doseCode", out JsonElement doseCode)
                && doseCode.GetString() == RouteCode;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return false;
        }
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private const string MedicationInstructions = """
        {
          "status": 200, "family": "SUCCESSFUL", "reason": "OK", "traceId": "e2e-trace",
          "data": {
            "methodUsages": [ { "id": "307166007", "name": "Before exercise", "code": "307166007", "nameLatin": "Before exercise (qualifier value)" } ],
            "drugUsages": [ { "id": "231271000", "name": "Intra-articular", "code": "231271000", "nameLatin": "Injection into joint (procedure)" } ],
            "drugInstructions": [ { "id": "2546009", "name": "Night", "code": "2546009", "nameLatin": "Night time (qualifier value)" } ],
            "formAmounts": [
              { "id": "24", "name": "Each", "code": "111", "nameLatin": "Each",
                "drugAmounts": [ { "id": "580", "name": "One quarter", "code": "580", "nameLatin": "1/4 Each" } ] }
            ]
          }
        }
        """;
}
