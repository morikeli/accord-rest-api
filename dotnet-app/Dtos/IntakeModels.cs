namespace AccordIntakeApi.Dtos;

// These DTOs mirror the business fields we need from the source ACORD XML and the
// final row we save to the shared aps_incoming table. 
// The original enterprise code was not available in the workspace, so this is the
// stripped-down model is used to preserve the flow: parse XML -> validate -> map -> persist.

public sealed class WorkOrder
{
    public string TransRefGuid { get; set; } = string.Empty;
    public string TransType { get; set; } = string.Empty;
    public string TransCode { get; set; } = string.Empty;
    public string TransExeDate { get; set; } = string.Empty;
    public string TransExeTime { get; set; } = string.Empty;
    public string TransMode { get; set; } = string.Empty;
    public string TransModeCode { get; set; } = string.Empty;
    public bool IsUpdate { get; set; }
    public bool IsCancelled { get; set; }
    public string TrackingId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public decimal FaceAmount { get; set; }
    public string RequirementCode { get; set; } = string.Empty;
    public string RequirementDetails { get; set; } = string.Empty;
    public string RequestedDate { get; set; } = string.Empty;
    public string RequirementAccountNumber { get; set; } = string.Empty;
    public string InsuredGovernmentId { get; set; } = string.Empty;
    public string InsuredFirstName { get; set; } = string.Empty;
    public string InsuredMiddleName { get; set; } = string.Empty;
    public string InsuredLastName { get; set; } = string.Empty;
    public string InsuredBirthDate { get; set; } = string.Empty;
    public string InsuredGender { get; set; } = string.Empty;
    public string InsuredStreet { get; set; } = string.Empty;
    public string InsuredCity { get; set; } = string.Empty;
    public string InsuredState { get; set; } = string.Empty;
    public string InsuredZip { get; set; } = string.Empty;
    public string InsuredCountry { get; set; } = string.Empty;
    public string InsuredPhone { get; set; } = string.Empty;
    public string InsuredEmail { get; set; } = string.Empty;
    public string PhysicianFirstName { get; set; } = string.Empty;
    public string PhysicianLastName { get; set; } = string.Empty;
    public string PhysicianFacility { get; set; } = string.Empty;
    public string PhysicianStreet { get; set; } = string.Empty;
    public string PhysicianCity { get; set; } = string.Empty;
    public string PhysicianState { get; set; } = string.Empty;
    public string PhysicianZip { get; set; } = string.Empty;
    public string PhysicianCountry { get; set; } = string.Empty;
    public string PhysicianPhone { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string AgentPhone { get; set; } = string.Empty;
    public string AgentEmail { get; set; } = string.Empty;
    public string CompanyProducerId { get; set; } = string.Empty;
    public string AttachmentType { get; set; } = string.Empty;
    public string AttachmentMimeType { get; set; } = string.Empty;
    public string AttachmentLocation { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class ApsIncomingEntity
{
    public long Id { get; set; }
    public string TrackingId { get; set; } = string.Empty;
    public string TransRefGuid { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public string PatientDateOfBirth { get; set; } = string.Empty;
    public string PatientGovernmentId { get; set; } = string.Empty;
    public string PatientStreet { get; set; } = string.Empty;
    public string PatientCity { get; set; } = string.Empty;
    public string PatientState { get; set; } = string.Empty;
    public string PatientZip { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public string DoctorFacility { get; set; } = string.Empty;
    public string DoctorStreet { get; set; } = string.Empty;
    public string DoctorCity { get; set; } = string.Empty;
    public string DoctorState { get; set; } = string.Empty;
    public string DoctorZip { get; set; } = string.Empty;
    public string DoctorPhone { get; set; } = string.Empty;
    public string CopyInstructions { get; set; } = string.Empty;
    public decimal PolicyAmount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public string Source { get; set; } = "xml";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

}

public sealed record IntakeResult(WorkOrder WorkOrder, ApsIncomingEntity ApsIncomingEntity, IReadOnlyList<string> ValidationErrors);
