# ACORD TXLife intake implementation

## Scope

The ZIP remains unchanged. This application is an adapted runnable host for the live assessment flow. The copied legacy controller and model files remain in the project for reference, but they depend on production services that were not included in the assessment. The runnable path is implemented in `Services/AcordXmlIntakeService.cs`.

## Live flow

1. `POST /intake/xml` accepts the supplied `OrderRequest.xml` as `application/xml`.
2. The service finds `TXLife/TXLifeRequest/OLifE` and maps transaction metadata.
3. It maps `Holding/Policy`, `Life`, `ApplicationInfo`, and `RequirementInfo` into `WorkOrder`.
4. It resolves parties through `AppliesToPartyID` and `Relation/@RelatedObjectID` for the insured, physician, and agent.
5. It validates required identifiers and distinguishes original, update, and cancellation requests using `TransMode` and its `tc` attribute.
6. It maps the WorkOrder into `ApsIncomingEntity`.
7. The entity is saved to the shared PostgreSQL `aps_incoming` table.
8. The response shows the WorkOrder, mapped entity, and validation errors.

## XML-to-table mapping

| XML node | WorkOrder | Database column |
| --- | --- | --- |
| `TXLifeRequest/TransRefGUID` | `TransRefGuid` | `trans_ref_guid` |
| `Policy/PolNumber` | `PolicyNumber` | `policy_number` |
| `Policy/Life/FaceAmt` | `FaceAmount` | `policy_amount` |
| `Policy/ApplicationInfo/TrackingID` | `TrackingId` | `tracking_id` |
| `RequirementInfo/RequirementDetails` | `RequirementDetails` | `copy_instructions` |
| insured `Person`, `Address`, `Phone`, `EMailAddress` | `Insured*` | `patient_*` |
| physician relation party | `Physician*` | `doctor_*` |
| `RequirementInfo/Attachment` | attachment fields | validation/output metadata |

## Shared database

PostgreSQL runs in the `acord-postgres` Docker container. This workspace maps container port 5432 to host port 5433 because host port 5432 was already occupied. Both apps connect to database `acord` on `localhost:5433`, using the development credentials in the local configuration. The table is `aps_incoming`; Docker creates it from `database/init.sql`, and the .NET startup check also creates it if it is missing. The Django model uses `managed = False` so Django does not attempt to recreate the table.

Start PostgreSQL, and backend services from the workspace root:

```bash
docker compose up --build
```

## Testing .NET app

```bash
curl -X POST http://localhost:5173/intake/xml \
  -H 'Content-Type: application/xml' \
  --data-binary @payloads/OrderRequest.xml
curl http://localhost:5173/intake/records
```

The sample is an original request, so the expected validation error list is empty. The returned WorkOrder contains tracking ID `d457994f-9ba0-40e6-8875-47a0bc6f9903`, policy `39132584`, and face amount `700000`; the mapped patient is Shaggy Doo and the physician is HARDWIN SMITH MD at Kaiser of Anaheim.

## Django JSON companion

`django-app/` contains a small Django app with `POST /intake/json`. It accepts the flattened equivalent of the XML sample in `payloads/sample.json`. Its unmanaged `ApsIncoming` model points to the same `aps_incoming` table and SQLite file. Install and run it with:

```bash
curl -X POST http://127.0.0.1:8001/api/aps \
  -H 'Content-Type: application/json' \
  --data-binary @payloads/OrderRequest.json
```

The JSON request should return `saved: true` and an empty error message. Querying the .NET `/intake/records` endpoint afterward shows both the XML and JSON rows in the same PostgreSQL table, with `source` identifying the input format.

## Deliberate stubs/adaptations

- Production persistence and email services were unavailable, so persistence is a local PostgreSQL Docker container and no external API is called.
- The old `GenericACORDController` and legacy model files are excluded from compilation because they require the missing enterprise `WorkOrder`, `Config`, `ConversionTools`, `EIS`, and email assemblies. Their behavior is represented by the focused adapted service.
- The supplied PDF attachment data is treated as metadata; it is not decoded or sent anywhere.
