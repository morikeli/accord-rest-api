using AccordIntakeApi.Dtos;
using Npgsql;

namespace AccordIntakeApi.Repository;

// Persists the mapped ACORD intake record into PostgreSQL in a shared table named aps_incoming.
// This was added to include a Dockerized database setup that can be shared across services.
public sealed class PostgresIntakeRepository
{
    private readonly string _connectionString;

    public PostgresIntakeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SharedDatabase")
            ?? "Host=localhost;Port=5433;Database=acord;Username=acord;Password=acord_dev_password";
    }

    // Ensures the shared PostgreSQL table exists before any XML record is inserted.
    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS aps_incoming (
                id BIGSERIAL PRIMARY KEY,
                tracking_id TEXT NOT NULL,
                trans_ref_guid TEXT NOT NULL,
                policy_number TEXT NOT NULL,
                patient_first_name TEXT NOT NULL,
                patient_last_name TEXT NOT NULL,
                patient_date_of_birth TEXT NOT NULL,
                patient_government_id TEXT NOT NULL,
                patient_street TEXT NOT NULL,
                patient_city TEXT NOT NULL,
                patient_state TEXT NOT NULL,
                patient_zip TEXT NOT NULL,
                patient_phone TEXT NOT NULL,
                patient_email TEXT NOT NULL,
                doctor_first_name TEXT NOT NULL,
                doctor_last_name TEXT NOT NULL,
                doctor_facility TEXT NOT NULL,
                doctor_street TEXT NOT NULL,
                doctor_city TEXT NOT NULL,
                doctor_state TEXT NOT NULL,
                doctor_zip TEXT NOT NULL,
                doctor_phone TEXT NOT NULL,
                copy_instructions TEXT NOT NULL,
                policy_amount NUMERIC(18, 2) NOT NULL,
                error_message TEXT NOT NULL,
                is_error BOOLEAN NOT NULL,
                source TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    // Inserts one normalized intake row and returns the generated database id so the caller can
    // inspect or trace the persisted record after the XML is processed.
    public async Task<long> InsertAsync(ApsIncomingEntity entity, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aps_incoming (
                tracking_id, trans_ref_guid, policy_number, patient_first_name, patient_last_name,
                patient_date_of_birth, patient_government_id, patient_street, patient_city, patient_state,
                patient_zip, patient_phone, patient_email, doctor_first_name, doctor_last_name, doctor_facility,
                doctor_street, doctor_city, doctor_state, doctor_zip, doctor_phone, copy_instructions,
                policy_amount, error_message, is_error, source, created_at, updated_at
            ) VALUES (
                @tracking_id, @trans_ref_guid, @policy_number, @patient_first_name, @patient_last_name,
                @patient_date_of_birth, @patient_government_id, @patient_street, @patient_city, @patient_state,
                @patient_zip, @patient_phone, @patient_email, @doctor_first_name, @doctor_last_name, @doctor_facility,
                @doctor_street, @doctor_city, @doctor_state, @doctor_zip, @doctor_phone, @copy_instructions,
                @policy_amount, @error_message, @is_error, @source, @created_at, @updated_at
            )
            RETURNING id;
            """;
        AddParameters(command, entity);
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    // Retrieves all intake rows from the shared table for review and debugging.
    public IReadOnlyList<ApsIncomingEntity> GetAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM aps_incoming ORDER BY id";
        using var reader = command.ExecuteReader();
        var records = new List<ApsIncomingEntity>();
        while (reader.Read()) records.Add(Read(reader));
        return records;
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddParameters(NpgsqlCommand command, ApsIncomingEntity entity)
    {
        command.Parameters.AddWithValue("tracking_id", entity.TrackingId);
        command.Parameters.AddWithValue("trans_ref_guid", entity.TransRefGuid);
        command.Parameters.AddWithValue("policy_number", entity.PolicyNumber);
        command.Parameters.AddWithValue("patient_first_name", entity.PatientFirstName);
        command.Parameters.AddWithValue("patient_last_name", entity.PatientLastName);
        command.Parameters.AddWithValue("patient_date_of_birth", entity.PatientDateOfBirth);
        command.Parameters.AddWithValue("patient_government_id", entity.PatientGovernmentId);
        command.Parameters.AddWithValue("patient_street", entity.PatientStreet);
        command.Parameters.AddWithValue("patient_city", entity.PatientCity);
        command.Parameters.AddWithValue("patient_state", entity.PatientState);
        command.Parameters.AddWithValue("patient_zip", entity.PatientZip);
        command.Parameters.AddWithValue("patient_phone", entity.PatientPhone);
        command.Parameters.AddWithValue("patient_email", entity.PatientEmail);
        command.Parameters.AddWithValue("doctor_first_name", entity.DoctorFirstName);
        command.Parameters.AddWithValue("doctor_last_name", entity.DoctorLastName);
        command.Parameters.AddWithValue("doctor_facility", entity.DoctorFacility);
        command.Parameters.AddWithValue("doctor_street", entity.DoctorStreet);
        command.Parameters.AddWithValue("doctor_city", entity.DoctorCity);
        command.Parameters.AddWithValue("doctor_state", entity.DoctorState);
        command.Parameters.AddWithValue("doctor_zip", entity.DoctorZip);
        command.Parameters.AddWithValue("doctor_phone", entity.DoctorPhone);
        command.Parameters.AddWithValue("copy_instructions", entity.CopyInstructions);
        command.Parameters.AddWithValue("policy_amount", entity.PolicyAmount);
        command.Parameters.AddWithValue("error_message", entity.ErrorMessage);
        command.Parameters.AddWithValue("is_error", entity.IsError);
        command.Parameters.AddWithValue("source", entity.Source);
        command.Parameters.AddWithValue("created_at", entity.CreatedAt);
        command.Parameters.AddWithValue("updated_at", entity.UpdatedAt);
    }

    private static ApsIncomingEntity Read(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("id")),
        TrackingId = reader.GetString(reader.GetOrdinal("tracking_id")),
        TransRefGuid = reader.GetString(reader.GetOrdinal("trans_ref_guid")),
        PolicyNumber = reader.GetString(reader.GetOrdinal("policy_number")),
        PatientFirstName = reader.GetString(reader.GetOrdinal("patient_first_name")),
        PatientLastName = reader.GetString(reader.GetOrdinal("patient_last_name")),
        PatientDateOfBirth = reader.GetString(reader.GetOrdinal("patient_date_of_birth")),
        PatientGovernmentId = reader.GetString(reader.GetOrdinal("patient_government_id")),
        PatientStreet = reader.GetString(reader.GetOrdinal("patient_street")),
        PatientCity = reader.GetString(reader.GetOrdinal("patient_city")),
        PatientState = reader.GetString(reader.GetOrdinal("patient_state")),
        PatientZip = reader.GetString(reader.GetOrdinal("patient_zip")),
        PatientPhone = reader.GetString(reader.GetOrdinal("patient_phone")),
        PatientEmail = reader.GetString(reader.GetOrdinal("patient_email")),
        DoctorFirstName = reader.GetString(reader.GetOrdinal("doctor_first_name")),
        DoctorLastName = reader.GetString(reader.GetOrdinal("doctor_last_name")),
        DoctorFacility = reader.GetString(reader.GetOrdinal("doctor_facility")),
        DoctorStreet = reader.GetString(reader.GetOrdinal("doctor_street")),
        DoctorCity = reader.GetString(reader.GetOrdinal("doctor_city")),
        DoctorState = reader.GetString(reader.GetOrdinal("doctor_state")),
        DoctorZip = reader.GetString(reader.GetOrdinal("doctor_zip")),
        DoctorPhone = reader.GetString(reader.GetOrdinal("doctor_phone")),
        CopyInstructions = reader.GetString(reader.GetOrdinal("copy_instructions")),
        PolicyAmount = reader.GetDecimal(reader.GetOrdinal("policy_amount")),
        ErrorMessage = reader.GetString(reader.GetOrdinal("error_message")),
        IsError = reader.GetBoolean(reader.GetOrdinal("is_error")),
        Source = reader.GetString(reader.GetOrdinal("source")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))

    };
}