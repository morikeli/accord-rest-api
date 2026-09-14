using System.Globalization;
using System.Xml.Linq;
using AccordIntakeApi.Dtos;
using AccordIntakeApi.Repository;
using IntakeWorkOrder = AccordIntakeApi.Dtos.WorkOrder;

namespace AccordIntakeApi.Services;

// This service adapts the original ACORD TXLife XML format into a simpler internal model
// and then writes the mapped record to the shared database. It keeps the required validation
// checks and extracts the required fields.
public sealed class AcordXmlIntakeService(PostgresIntakeRepository repository)
{
    // private readonly PostgresIntakeRepository _repository;

    // public AcordXmlIntakeService(PostgresIntakeRepository repository)
    // {
    //     _repository = repository;
    // }

    // Entry point for XML intake requests. The method loads the ACORD document, pulls out the
    // relevant TXLife sections, translates them to a WorkOrder, validates the required fields,
    // and stores the mapped row before returning the result payload.
    public async Task<IntakeResult> ProcessAsync(Stream xmlStream, CancellationToken cancellationToken)
    {
        var document = await XDocument.LoadAsync(xmlStream, LoadOptions.None, cancellationToken);
        var request = document.Root?.Element("TXLifeRequest")
            ?? throw new FormatException("The XML must contain TXLife/TXLifeRequest.");
        var olife = request.Element("OLifE")
            ?? throw new FormatException("The TXLifeRequest must contain OLifE.");
        var holding = olife.Element("Holding");
        var policy = holding?.Element("Policy");
        var requirement = policy?.Element("RequirementInfo");
        var insured = FindParty(olife, requirement?.Attribute("AppliesToPartyID")?.Value ?? "");
        var physician = FindPartyByRelation(olife, "Physician");
        var agent = FindPartyByRelation(olife, "Agent");

        var workOrder = new IntakeWorkOrder
        {
            TransRefGuid = Value(request, "TransRefGUID"),
            TransType = Value(request, "TransType"),
            TransCode = Attribute(request.Element("TransType"), "tc"),
            TransExeDate = Value(request, "TransExeDate"),
            TransExeTime = Value(request, "TransExeTime"),
            TransMode = Value(request, "TransMode"),
            TransModeCode = Attribute(request.Element("TransMode"), "tc"),
            IsCancelled = Attribute(request.Element("TransMode"), "tc") == "6" || Value(request, "TransMode").Equals("Cancellation", StringComparison.OrdinalIgnoreCase),
            TrackingId = Value(policy?.Element("ApplicationInfo"), "TrackingID"),
            PolicyNumber = Value(policy, "PolNumber"),
            FaceAmount = DecimalValue(Value(holding?.Element("Policy")?.Element("Life"), "FaceAmt")),
            RequirementCode = Value(requirement, "ReqCode"),
            RequirementDetails = Value(requirement, "RequirementDetails"),
            RequestedDate = Value(requirement, "RequestedDate"),
            RequirementAccountNumber = Value(requirement, "RequirementAcctNum"),
            InsuredGovernmentId = Value(insured, "GovtID"),
            InsuredFirstName = Value(insured?.Element("Person"), "FirstName"),
            InsuredMiddleName = Value(insured?.Element("Person"), "MiddleName"),
            InsuredLastName = Value(insured?.Element("Person"), "LastName"),
            InsuredBirthDate = Value(insured?.Element("Person"), "BirthDate"),
            InsuredGender = Value(insured?.Element("Person"), "Gender"),
            InsuredStreet = Value(insured?.Element("Address"), "Line1"),
            InsuredCity = Value(insured?.Element("Address"), "City"),
            InsuredState = Value(insured?.Element("Address"), "AddressState"),
            InsuredZip = Value(insured?.Element("Address"), "Zip"),
            InsuredCountry = Value(insured?.Element("Address"), "AddressCountry"),
            InsuredPhone = Value(insured?.Element("Phone"), "DialNumber"),
            InsuredEmail = Value(insured?.Element("EMailAddress"), "AddrLine"),
            PhysicianFirstName = Value(physician?.Element("Person"), "FirstName"),
            PhysicianLastName = Value(physician?.Element("Person"), "LastName"),
            PhysicianFacility = Value(physician, "FullName"),
            PhysicianStreet = Value(physician?.Element("Address"), "Line1"),
            PhysicianCity = Value(physician?.Element("Address"), "City"),
            PhysicianState = Value(physician?.Element("Address"), "AddressStateTC"),
            PhysicianZip = Value(physician?.Element("Address"), "Zip"),
            PhysicianCountry = Value(physician?.Element("Address"), "AddressCountry"),
            PhysicianPhone = Value(physician?.Element("Phone"), "DialNumber"),
            AgentName = Value(agent, "FullName"),
            AgentPhone = Value(agent?.Element("Phone"), "DialNumber"),
            AgentEmail = Value(agent?.Element("EMailAddress"), "AddrLine"),
            CompanyProducerId = Value(olife.Descendants("CompanyProducerID").FirstOrDefault(), null),
            AttachmentType = Value(requirement?.Element("Attachment"), "AttachmentBasicType"),
            AttachmentMimeType = Value(requirement?.Element("Attachment"), "MimeTypeTC"),
            AttachmentLocation = Value(requirement?.Element("Attachment"), "AttachmentLocation")
        };

        workOrder.IsUpdate = workOrder.TransModeCode == "4" || workOrder.TransMode.Equals("Update", StringComparison.OrdinalIgnoreCase);
        var validationErrors = Validate(workOrder);
        workOrder.ErrorMessage = string.Join("; ", validationErrors);
        var entity = MapToEntity(workOrder, validationErrors.Count > 0);
        entity.Id = await repository.InsertAsync(entity, cancellationToken);

        return new IntakeResult(workOrder, entity, validationErrors);
    }

    // Keeps the required checks aligned to the fields that matter in the sample XML.
    // Missing transaction metadata or required insured information is treated as a validation error.
    private static List<string> Validate(IntakeWorkOrder workOrder)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(workOrder.TransRefGuid)) errors.Add("Missing TransRefGUID");
        if (string.IsNullOrWhiteSpace(workOrder.TrackingId)) errors.Add("Missing ApplicationInfo/TrackingID");
        if (string.IsNullOrWhiteSpace(workOrder.InsuredGovernmentId)) errors.Add("Missing insured GovtID");
        if (string.IsNullOrWhiteSpace(workOrder.InsuredFirstName) && string.IsNullOrWhiteSpace(workOrder.InsuredLastName)) errors.Add("Missing insured name");
        if (workOrder.IsUpdate || workOrder.IsCancelled) return errors;
        if (string.IsNullOrWhiteSpace(workOrder.RequirementCode)) errors.Add("Missing RequirementInfo/ReqCode");
        return errors;
    }

    // Maps the normalized work order into the shared row shape required by the database table.
    private static ApsIncomingEntity MapToEntity(IntakeWorkOrder workOrder, bool isError) => new()
    {
        TrackingId = workOrder.TrackingId,
        TransRefGuid = workOrder.TransRefGuid,
        PolicyNumber = workOrder.PolicyNumber,
        PatientFirstName = workOrder.InsuredFirstName,
        PatientLastName = workOrder.InsuredLastName,
        PatientDateOfBirth = workOrder.InsuredBirthDate,
        PatientGovernmentId = workOrder.InsuredGovernmentId,
        PatientStreet = workOrder.InsuredStreet,
        PatientCity = workOrder.InsuredCity,
        PatientState = workOrder.InsuredState,
        PatientZip = workOrder.InsuredZip,
        PatientPhone = workOrder.InsuredPhone,
        PatientEmail = workOrder.InsuredEmail,
        DoctorFirstName = workOrder.PhysicianFirstName,
        DoctorLastName = workOrder.PhysicianLastName,
        DoctorFacility = workOrder.PhysicianFacility,
        DoctorStreet = workOrder.PhysicianStreet,
        DoctorCity = workOrder.PhysicianCity,
        DoctorState = workOrder.PhysicianState,
        DoctorZip = workOrder.PhysicianZip,
        DoctorPhone = workOrder.PhysicianPhone,
        CopyInstructions = workOrder.RequirementDetails,
        PolicyAmount = workOrder.FaceAmount,
        ErrorMessage = workOrder.ErrorMessage,
        IsError = isError,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow

    };

    private static XElement? FindParty(XElement olife, string id) => olife.Elements("Party").FirstOrDefault(p => p.Attribute("id")?.Value == id);

    private static XElement? FindPartyByRelation(XElement olife, string role)
    {
        var relation = olife.Elements("Relation").FirstOrDefault(r => Value(r, "RelationRoleCode").Equals(role, StringComparison.OrdinalIgnoreCase));
        var partyId = relation?.Attribute("RelatedObjectID")?.Value;
        return FindParty(olife, partyId ?? string.Empty);
    }

    private static string Value(XElement? parent, string? childName)
    {
        if (parent is null) return string.Empty;
        return childName is null ? parent.Value.Trim() : (parent.Element(childName)?.Value.Trim() ?? string.Empty);
    }

    private static string Attribute(XElement? element, string name) => element?.Attribute(name)?.Value.Trim() ?? string.Empty;

    private static decimal DecimalValue(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
}
