using FluentAssertions;
using Moq;
using Tamin.Integration;
using Tamin.Integration.Mapping;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminPrescriptionMapperTests
{
    private readonly TaminPrescriptionMapper _mapper = new();

    [Fact]
    public async Task Drug_item_is_sent_with_the_required_service_type()
    {
        PrescriptionCreateInput sent = await RegisterAsync(Item(PrescriptionItemCategory.Drug, "00125", metadataJson: null));

        sent.PrescType!.PrescTypeId.Should().Be(1);
        sent.Terminology.Should().BeNull();
        PrescriptionDetailInput detail = sent.NoteDetailEprscs!.Single();
        detail.SrvId!.SrvCode.Should().Be("00125");
        detail.SrvId.SrvType.Should().Be("01");
    }

    [Fact]
    public async Task Snomed_drug_values_come_from_item_metadata_and_mark_the_prescription_terminology()
    {
        PrescriptionItem drug = Item(
            PrescriptionItemCategory.Drug,
            "00125",
            """{ "drugAmntId": 580, "drugInstId": 2546009, "doseCode": "231271000", "methodUsageId": 12 }""");
        drug.DosageJson = "free text";

        PrescriptionCreateInput sent = await RegisterAsync(drug);

        sent.Terminology.Should().Be("SNOMED");
        PrescriptionDetailInput detail = sent.NoteDetailEprscs!.Single();
        detail.TimesAday!.DrugAmntId.Should().Be(580);
        detail.DrugInstruction!.DrugInstId.Should().Be(2546009);
        detail.DoseCode.Should().Be("231271000");
        detail.MethodUsage!.Id.Should().Be(12);
        detail.Dose.Should().BeNull("the route travels as doseCode for SNOMED drugs");
    }

    [Fact]
    public async Task Legacy_drug_keeps_free_text_dose_and_sends_no_terminology()
    {
        PrescriptionItem drug = Item(PrescriptionItemCategory.Drug, "00125", """{ "drugAmntId": 7 }""");
        drug.DosageJson = "2 daily";

        PrescriptionCreateInput sent = await RegisterAsync(drug);

        sent.Terminology.Should().BeNull();
        sent.NoteDetailEprscs!.Single().Dose.Should().Be("2 daily");
        sent.NoteDetailEprscs!.Single().TimesAday!.DrugAmntId.Should().Be(7);
    }

    [Theory]
    [InlineData(PrescriptionItemCategory.MedicalService, "17")]
    [InlineData(PrescriptionItemCategory.Physiotherapy, "17")]
    public async Task Non_drug_items_default_to_the_medical_service_type(PrescriptionItemCategory category, string expected)
    {
        PrescriptionCreateInput sent = await RegisterAsync(Item(category, "S1", metadataJson: null));

        sent.NoteDetailEprscs!.Single().SrvId!.SrvType.Should().Be(expected);
    }

    [Fact]
    public async Task Item_metadata_can_name_a_paraclinical_service_type()
    {
        PrescriptionCreateInput sent = await RegisterAsync(
            Item(PrescriptionItemCategory.Paraclinic, "L1", """{ "serviceType": "03" }"""));

        sent.NoteDetailEprscs!.Single().SrvId!.SrvType.Should().Be("03");
    }

    [Fact]
    public async Task Doctor_mobile_is_sent_as_the_doctor_contact()
    {
        PrescriptionCreateInput sent = await RegisterAsync(Item(PrescriptionItemCategory.Drug, "1", null));

        sent.DocMobileNo.Should().Be("09121111111");
    }

    [Fact]
    public async Task Missing_patient_mobile_is_omitted_instead_of_blocking_submission()
    {
        PrescriptionCreateInput sent = await RegisterAsync(Item(PrescriptionItemCategory.Drug, "1", null), mobile: null);

        sent.Mobile.Should().BeNull();
    }

    [Fact]
    public async Task Visit_prescription_sends_no_items()
    {
        PrescriptionCreateInput sent = await RegisterAsync(Item(PrescriptionItemCategory.Visit, "V1", null));

        sent.PrescType!.PrescTypeId.Should().Be(3);
        sent.NoteDetailEprscs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{ "drugAmntId": 580, "drugInstId": 2546009 }""", null)]
    [InlineData("""{ "drugAmntId": 580 }""", "Drug A")]
    [InlineData("""{ "drugInstId": 2546009 }""", "Drug A")]
    [InlineData(null, "Drug A")]
    public void Drug_without_amount_and_time_is_reported_by_name(string? metadataJson, string? expected)
    {
        PrescriptionItem drug = Item(PrescriptionItemCategory.Drug, "1", metadataJson);
        drug.DisplayName = "Drug A";

        TaminPrescriptionMapper.FindDrugMissingConsumption([drug]).Should().Be(expected);
    }

    [Fact]
    public void Non_drug_items_never_need_consumption_values()
    {
        TaminPrescriptionMapper.FindDrugMissingConsumption(
            [Item(PrescriptionItemCategory.MedicalService, "S1", null)]).Should().BeNull();
    }

    private async Task<PrescriptionCreateInput> RegisterAsync(PrescriptionItem item, string? mobile = "09120000000")
    {
        PrescriptionCreateInput? sent = null;
        Mock<ITaminClient> client = new();
        _ = client
            .Setup(c => c.CreatePrescriptionAsync(It.IsAny<PrescriptionCreateInput>(), It.IsAny<CancellationToken>()))
            .Callback<PrescriptionCreateInput, CancellationToken>((input, _) => sent = input)
            .ReturnsAsync(new PrescriptionMutationResult(11, null, null, null, 22));
        PrescriptionRecord prescription = new()
        {
            PatientNationalId = "1111111111",
            SubmittedAt = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc),
            Items = [item]
        };

        _ = await _mapper.RegisterPrescriptionAsync(
            client.Object, prescription, "doc", "doc-national", "09121111111", "siam", mobile, new TaminDateFormatter(), CancellationToken.None);

        return sent!;
    }

    private static PrescriptionItem Item(PrescriptionItemCategory category, string code, string? metadataJson) => new()
    {
        Id = Guid.NewGuid(),
        PrescriptionRecordId = Guid.NewGuid(),
        Category = category,
        ReferenceCode = code,
        DisplayName = "item",
        Quantity = 1,
        MetadataJson = metadataJson
    };
}
