# ACORD TXLife Intake Flow

## Overview

The supplied code is a partial/legacy ACORD TXLife intake implementation. The main entry point is `GenericACORDController`, which receives an XML document, locates the `TXLifeRequest` nodes, converts each request into a `WorkOrder`, validates the resulting `WorkOrder`, performs some data correction, and provides a `WorkOrderMapper` for converting the `WorkOrder` into an `APSIncomingEntity`.

The supplied files do not contain the complete production application. Some dependencies such as `WorkOrder`, `APSIncomingEntity`, `XMLUtility`, `ConversionTools`, `Config`, and supporting constants/services are referenced but are not included.

The original database persistence code is commented out, so the supplied code does not itself perform the final database write.

---

## High-Level Flow

```text
XML payload
    |
    v
ProcessXMLFile()
    |
    |-- Remove XML namespaces
    |-- Load XML into XmlDocument
    |-- Find TXLife/TXLifeRequest nodes
    v
ProcessRequestObject()
    |
    v
CreateWorkOrder()
    |
    |-- Parse TXLifeRequest
    |-- Determine transaction type
    |-- Parse RequirementInfo
    |-- Parse Holding / Policy / Life
    |-- Parse insured Party
    |-- Parse physician/facility Party
    |-- Parse requester Party
    |-- Parse writing agent
    |-- Parse Relations
    v
WorkOrder
    |
    v
Validation
    |
    |-- New request -> Validate()
    |-- Update/cancel -> ValidateUpdateCancel()
    |
    v
CorrectData()
    |
    v
WorkOrderMapper()
    |
    v
APSIncomingEntity
    |
    v
[Database persistence must be implemented]
```

---

# XML Parsing

The main XML entry point is:

```csharp
ProcessXMLFile(string accordxml)
```

The method first removes namespaces from the incoming XML:

```csharp
string newAccordxml =
    new XMLUtility().RemoveNamespacesFromXML(accordxml);
```

The resulting XML is loaded into an `XmlDocument`:

```csharp
XmlDocument dom = new XmlDocument();
dom.LoadXml(newAccordxml);
```

The controller then searches for all `TXLifeRequest` nodes:

```csharp
XmlNodeList nodelist =
    dom.DocumentElement.SelectNodes("//TXLife/TXLifeRequest");
```

These nodes are passed to:

```csharp
ProcessRequestObject(nodelist, nsMgr);
```

If no requests are found, an empty list is returned and `ProcessXMLFile()` returns `null`.

Otherwise, `ProcessXMLFile()` returns the first generated `WorkOrder`.

### Important observation

The supplied code removes namespaces before using XPath. The comment indicates this is done so that the code does not have to deal with namespace-related XPath issues.

---

# TXLifeRequest Mapping

Each `TXLifeRequest` is initially processed by `CreateWorkOrder()`.

A `TXLifeRequest` object is created:

```csharp
alife = new TXLifeRequest();
alife.ProcessXML(tmpNode);
```

`TXLifeRequest.ProcessXML(XmlNode)` reads the direct children of the `TXLifeRequest` node.

The following XML elements are mapped:

| XML element         | TXLifeRequest property |
| ------------------- | ---------------------- |
| `TransRefGUID`      | `TransRefGUID`         |
| `TransType` text    | `TransType`            |
| `TransType/@tc`     | `TransCode`            |
| `TransExeDate`      | `TransExeDate`         |
| `TransExeTime`      | `TransExeTime`         |
| `TransMode` text    | `TransMode`            |
| `TransMode/@tc`     | `TransModeCode`        |
| `TestIndicator`     | `TestIndicator`        |
| `TestIndicator/@tc` | `TestIndicatorCode`    |

The transaction information is then copied into the `WorkOrder`:

```csharp
wo.TransExeDate = alife.TransExeDate;
wo.TransExeTime = alife.TransExeTime;
wo.TransRefGUID = alife.TransRefGUID;
wo.TestOnly = alife.TestIndicator;
wo.TransCode = alife.TransCode;
```

---

# Determining New, Update, or Cancellation

The controller determines whether the request is a cancellation or update using the transaction mode/code.

### Cancellation

A request is considered cancelled when either:

```csharp
alife.TransModeCode == "6"
```

or:

```csharp
alife.TransMode.Trim().ToUpper() == "CANCELLATION"
```

The controller then sets:

```csharp
wo.IsCancelled = true;
```

### Update

A request is considered an update when either:

```csharp
alife.TransCode == "4"
```

or:

```csharp
alife.TransMode.Trim().ToUpper() == "UPDATE"
```

The controller then sets:

```csharp
wo.IsUpdate = true;
```

If neither condition is satisfied, the request follows the normal/new-order validation path.

---

# Relation Mapping

`CreateWorkOrder()` creates a `TXLifeRequestOLifERelation` object and processes the XML:

```csharp
relation.ProcessXML(tmpNode);
wo.RelationNodeXML = relation.RelationNodeXML;
```

The relation information is therefore retained as XML on the `WorkOrder`.

The sample XML contains relations connecting:

* Holding → Agent
* Holding → Insured
* Insured → Physician
* Holding → Home Office

These relationships use `OriginatingObjectID`, `RelatedObjectID`, and `RelationRoleCode`.

---

# RequirementInfo Mapping

The controller creates:

```csharp
TXLifeTXLifeRequestOLifEHoldingPolicyRequirementInfo req
```

and processes the request XML.

The resulting values are copied to the `WorkOrder`.

Important mappings include:

| RequirementInfo field     | WorkOrder                               |
| ------------------------- | --------------------------------------- |
| `RequesterPartyID`        | `RequesterPartyID`                      |
| `AppliesToPartyID`        | `AppliesToPartyID`                      |
| `FulfillerPartyID`        | `FullfillerPartyID`                     |
| `RequestorContactPartyID` | `RequestorContactPartyID`               |
| `RequirementInfoUniqueID` | `RequestID` / `RequirementInfoUniqueID` |
| `ReqCode`                 | `CopyInstruction`                       |
| `ReqCode/@tc`             | `ReqCodeTC`                             |
| `DeliveryInstructionDesc` | `NoteToEIS`                             |
| `FeeCapAmt`               | `MaxFee`                                |
| `UnitCode`                | `UnitCode`                              |
| `RequirementDetails`      | `Note`                                  |
| `RequestedDate`           | `RequestedDate`                         |
| `ScheduledDate`           | `ScheduledDate`                         |
| `RequirementAcctNum`      | `RequirementAcctNum`                    |
| Application tracking ID   | `ApplicationInfoTrackingID`             |

If the requirement code represents an exam, `IsExam` is set to `"1"`.

If the requirement priority is `RUSH`, the controller sets:

```csharp
wo.IsUrgent = true;
```

---

# Holding, Life and Policy Mapping

The controller processes the `Holding` node:

```csharp
holding.ProcessXML(tmpNode);
```

and copies:

```csharp
wo.HoldingTypeCode = holding.HoldingTypeCode;
wo.HoldingTypeCodeTC = holding.HoldingTypeCodeTC;
```

The `Life` information is processed next:

```csharp
life.ProcessXML(tmpNode);
wo.FaceAmount = life.FaceAmt;
```

The policy is then processed:

```csharp
policy.ProcessXML(tmpNode);
```

Important policy values include:

```text
CarrierPartyID
PolicyNumber
ProductType
ProductTypeTC
BillCode
```

These are mapped to corresponding `WorkOrder` properties.

The carrier party is then looked up using `CarrierPartyID` so that the insurance company information can be obtained.

---

# ApplicationInfo Mapping

The controller processes `ApplicationInfo`:

```csharp
appinfo.ProcessXML(tmpNode);
```

and maps the relevant application information into the `WorkOrder`.

The most important value for validation is the tracking ID:

```text
ApplicationInfo/TrackingID
        |
        v
WorkOrder.ApplicationInfoTrackingID
```

---

# Insured / Applicant Mapping

The `AppliesToPartyID` from `RequirementInfo` identifies the party to use as the applicant/insured.

The controller processes the party:

```csharp
party.PartyID = wo.AppliesToPartyID;
party.ProcessXML(tmpNode);
```

For a person (`PartyTypeCode == "1"`), the controller obtains:

* Government ID
* First name
* Middle name
* Last name
* Date of birth
* Gender

These are mapped to:

```text
ApplicantSSN
ApplicantFirstName
ApplicantMiddleInitial
ApplicantLastName
ApplicantDOB
Gender
```

---

# Applicant Address

The controller attempts to obtain an address for the applicant.

The preferred order is:

```text
Residence
    ↓
Other
    ↓
Unknown
    ↓
Business
    ↓
Work
```

The selected address is mapped into:

```text
ApplicantAddress
ApplicantCity
ApplicantState
ApplicantZip
```

This fallback behavior means the controller does not stop immediately if a residence address is unavailable.

---

# Applicant Phone and Email

The controller searches for the applicant's phone number using several phone types.

It first attempts to retrieve a phone without specifying a type and then tries additional phone types if necessary.

The resulting phone number is stored in:

```text
PatientPhone1
PatientPhone2
```

Phone numbers are also sanitized by removing `-`.

Values beginning with `"000"` are treated as invalid/empty.

For email, the controller first attempts to retrieve the preferred email address. If one is not available, it retrieves any available email address.

The result is stored as:

```text
PatientEmail
```

---

# Physician / Facility Mapping

The physician is identified through the doctor relation:

```csharp
TXLifeRequestOLifERelationDoctor relationdoctor
```

The related party ID is obtained from the relation:

```csharp
string doctorPartyID =
    party.PartyID = relationdoctor.RelatedObjectID;
```

The controller then processes the physician party.

For a person, the physician's:

```text
FirstName
LastName
```

are mapped to:

```text
DoctorFirstName
DoctorLastName
```

If `DoctorOrFacilityName` is empty, the party's `FullName` is used instead.

---

# Physician Address

The controller attempts to obtain the physician/facility address.

The search order is:

```text
Business
    ↓
Mailing
    ↓
Other
    ↓
Unknown
    ↓
Regional Office
    ↓
Work
    ↓
Residence
```

The selected address is mapped to:

```text
FacilityAddress
FacilityCity
FacilityState
FacilityZipCode
```

State codes may be converted using `Config.StateCodeList`.

---

# Physician Phone and Fax

The physician's phone number is retrieved using several possible phone types if the initial lookup does not return a value.

The resulting values are mapped to:

```text
FacilityPhone
FacilityPhoneExt
```

A business fax number is also retrieved and mapped to:

```text
FacilityFax
```

---

# Requester Mapping

The requester party is identified using:

```text
WorkOrder.RequesterPartyID
```

The controller processes that party and obtains the requester's company information:

```text
RequestorCompanyName
AgencyCarrierCode
CompanyProducerID
```

If a separate requester contact party exists, the controller attempts to obtain the requester's:

```text
Name
First name
Last name
Phone
Phone extension
Email
```

If no requester name is found, the company name is used as the requester name.

---

# Writing Agent Mapping

The writing agent is identified through:

```csharp
TXLifeRequestOLifERelationWritingAgent
```

The related party ID is used to retrieve the agent.

The controller maps:

```text
FirstName
LastName
CarrierCode
Email
Phone
Phone extension
Address
City
State
Zip
```

into the corresponding `WorkOrder` writing-agent properties.

---

# WorkOrder Validation

After `CreateWorkOrder()` returns, `ProcessRequestObject()` determines which validation method to use.

### New request

For a normal/new request:

```csharp
if (!tempwo.IsUpdate && !tempwo.IsCancelled)
    Validate(tempwo);
```

### Update or cancellation

For an update or cancellation:

```csharp
else if (tempwo.IsCancelled || tempwo.IsUpdate)
    ValidateUpdateCancel(tempwo);
```

---

## New Request Validation

`Validate()` checks the following required values:

```text
TransRefGUID
ApplicationInfoTrackingID
ApplicantFirstName
ApplicantLastName
ApplicantDOB
ApplicantSSN
```

If any are missing, an error message is added to `WorkOrder.ErrorMessage`.

For example:

```text
Missing TransRefGUID
Missing TrackingID
Missing Applicant First Name
Missing Applicant Last Name
Missing Applicant DOB
Missing Applicant SSN
```

The controller combines multiple errors into one error string.

Several additional validation checks exist in the source but are commented out. They are therefore not part of the live flow documented here.

---

## Update / Cancellation Validation

`ValidateUpdateCancel()` performs a smaller validation set.

It checks:

```text
TransRefGUID
ApplicationInfoTrackingID
```

If either is missing, the corresponding message is placed in:

```text
WorkOrder.ErrorMessage
```

---

# Cancellation Handling

Cancellation requests are handled specially in `ProcessRequestObject()`.

If:

```csharp
tempwo.IsCancelled
```

is true, the code reaches the cancellation branch.

The original cancellation service call is commented out:

```csharp
//dac.CancelOrder(tempwo.TransRefGUID);
```

The current live code uses:

```csharp
string resultcancel = "";
```

Therefore, in the supplied code, no external cancellation operation actually occurs.

The `WorkOrder` is added to the result list and processing continues.

---

# Data Correction

After validation, the controller calls:

```csharp
CorrectData(tempwo);
```

The purpose is to sanitize data received from external sources.

One visible example is state-code correction. If a state value is longer than two characters, `CorrectStateCode()` attempts to convert the value into the expected state abbreviation using `Config.StateCodeList`.

This correction is applied to relevant `WorkOrder` state fields.

---

# WorkOrderMapper

`WorkOrderMapper()` converts a `WorkOrder` into:

```csharp
BusinessEntities.APSIncomingEntity
```

It creates a new entity:

```csharp
BusinessEntities.APSIncomingEntity entity =
    new BusinessEntities.APSIncomingEntity();
```

It then maps WorkOrder properties into database/entity fields.

Examples include:

| WorkOrder                   | APSIncomingEntity         |
| --------------------------- | ------------------------- |
| `Note`                      | `CopyInstructions`        |
| `FacilityCity`              | `DoctorCity`              |
| `DoctorCountry`             | `DoctorCountry`           |
| `DoctorOrFacilityName`      | `DoctorFacility`          |
| `FacilityFax`               | `DoctorFax`               |
| `DoctorFirstName`           | `DoctorFirstName`         |
| `DoctorLastName`            | `DoctorLastName`          |
| `FacilityPhone`             | `DoctorPhone`             |
| `FacilityPhoneExt`          | `DoctorPhoneExtension`    |
| `FacilityState`             | `DoctorState`             |
| `FacilityZipCode`           | `DoctorZipCode`           |
| `FacilityAddress`           | `DoctorStreet1`           |
| `ErrorMessage`              | `ErrorMessage`            |
| `AttachmentLocation`        | `HIPPALocation`           |
| `TransExeDate`              | `OrderDate`               |
| `ApplicantCity`             | `PatientCity`             |
| `ApplicantDOB`              | `PatientDOB`              |
| `PatientEmail`              | `PatientEmail`            |
| `ApplicantFirstName`        | `PatientFirstName`        |
| `ApplicantLastName`         | `PatientLastName`         |
| `ApplicantMiddleInitial`    | `PatientMiddleName`       |
| `PatientPhone1`             | `PatientPhone1`           |
| `PatientPhone2`             | `PatientPhone2`           |
| `Gender`                    | `PatientGender`           |
| `ApplicantSSN`              | `PatientSSN`              |
| `ApplicantState`            | `PatientState`            |
| `ApplicantAddress`          | `PatientStreet1`          |
| `ApplicantZip`              | `PatientZipCode`          |
| `FaceAmount`                | `PolicyAmcount`           |
| `PolicyNumber`              | `PolicyNumber`            |
| `RequirementAcctNum`        | `RequirementAcctNum`      |
| `RequirementInfoUniqueID`   | `RequirementInfoUniqueID` |
| `ApplicationInfoTrackingID` | `TrackingID`              |
| `TransRefGUID`              | `TransRefGUID`            |
| `WritingAgentAddress`       | `WritingAgentAddress`     |
| `WritingAgentCity`          | `WritingAgentCity`        |
| `WritingAgentEmail`         | `WritingAgentEmail`       |
| `WritingAgentFirstName`     | `WritingAgentFirstName`   |
| `WritingAgentLastName`      | `WritingAgentLastName`    |
| `WritingAgentPhone`         | `WritingAgentPhone`       |
| `WritingAgentPhoneExt`      | `WritingAgentPhoneExt`    |
| `WritingAgentState`         | `WritingAgentState`       |
| `WritingAgentZipCode`       | `WritingAgentZipCode`     |
| `CarrierCode`               | `CarrierCode`             |
| `AgencyCarrierCode`         | `AgencyCarrierCode`       |
| `CompanyProducerID`         | `CompanyProducerID`       |
| `IsUrgent`                  | `IsUrgent`                |
| `InsuranceCompany`          | `DestinationCode`         |

The mapper also initializes several values directly, including:

```text
CompanyID = 1
IsTestOnly = false
SendToCompany = false
```

---

# Error Handling in WorkOrderMapper

The mapper copies the WorkOrder error message:

```csharp
entity.ErrorMessage =
    ConversionTools.CheckForNull(wo.ErrorMessage, "");
```

It then determines whether the entity is in an error state:

```csharp
if (wo.ErrorMessage.Length > 0)
    entity.IsError = true;
else
    entity.IsError = false;
```

`LoadToEISMain` is then determined from the test/error state:

```text
Not test + no error
        ↓
LoadToEISMain = true

Otherwise
        ↓
LoadToEISMain = false
```

---

# Order Date Handling

`WorkOrderMapper()` converts:

```text
WorkOrder.TransExeDate
```

into:

```text
APSIncomingEntity.OrderDate
```

If the resulting date is earlier than the minimum date (`1900-01-01`), the mapper falls back to:

```text
WorkOrder.ReceiveDate
```

If that is also invalid, it falls back to:

```text
DateTime.Now
```

---

# Important Implementation Gap

The supplied code does **not** currently perform the final database insertion.

The original insertion code in `ProcessRequestObject()` is commented out:

```csharp
//tempwo.WorkOrderID = dataprovider.InsertWorkOrder(tempwo);
```

Therefore, the live supplied flow ends after creating and validating the `WorkOrder`.

`WorkOrderMapper()` exists and produces an `APSIncomingEntity`, but the supplied controller does not show a live call from `ProcessRequestObject()` to `WorkOrderMapper()` followed by a database insert.

---

# Sample XML Flow

For the supplied `OrderRequest.xml`, the important structure is:

```text
TXLife
└── TXLifeRequest
    ├── TransRefGUID
    ├── TransType
    ├── TransExeDate
    ├── TransExeTime
    ├── TransMode
    └── OLifE
        ├── Holding
        │   └── Policy
        │       ├── PolNumber
        │       ├── Life
        │       │   └── FaceAmt
        │       ├── ApplicationInfo
        │       │   └── TrackingID
        │       └── RequirementInfo
        │           ├── ReqCode
        │           ├── RequirementDetails
        │           ├── HORequirementRefID
        │           ├── RequestedDate
        │           ├── ReleasePartyOrgCode
        │           └── RequirementAcctNum
        │
        ├── Party_Agency
        ├── Party_HomeOffice
        ├── Party_Agent
        ├── Party_Insured
        │   ├── GovtID
        │   ├── Person
        │   ├── Address
        │   ├── Phone
        │   └── EMailAddress
        │
        ├── Party_Physician
        │   ├── Person
        │   ├── FullName
        │   ├── Address
        │   └── Phone
        │
        └── Relation
            ├── holdingAgent
            ├── holdingInsured
            ├── insuredPhysician
            └── homeOffice
```

The sample contains an original request (`TransMode tc="2"`) and an APS requirement (`HORequirementRefID = 1`). The insured party is identified by `Party_Insured`, while the physician is connected through the `insuredPhysician` relation.

---

# Summary

The live processing flow can therefore be summarized as:

```text
1. Receive XML
       ↓
2. Remove namespaces
       ↓
3. Load XML into XmlDocument
       ↓
4. Find TXLifeRequest nodes
       ↓
5. Create WorkOrder
       ↓
6. Parse TXLifeRequest
       ↓
7. Determine new/update/cancellation
       ↓
8. Parse RequirementInfo
       ↓
9. Parse Holding/Policy/Life/ApplicationInfo
       ↓
10. Parse insured/applicant
       ↓
11. Parse physician/facility
       ↓
12. Parse requester
       ↓
13. Parse writing agent
       ↓
14. Preserve relation XML
       ↓
15. Validate WorkOrder
       ↓
16. Correct state/data values
       ↓
17. Return WorkOrder
       ↓
18. WorkOrderMapper can transform WorkOrder
    into APSIncomingEntity
       ↓
19. Store data in db
```

The main responsibility of the code is therefore to transform an ACORD `TXLifeRequest` XML structure into the application's internal `WorkOrder` representation, validate that representation according to the transaction type, and provide the mapping required to produce an `APSIncomingEntity`.
