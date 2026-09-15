CREATE TABLE IF NOT EXISTS aps_incoming (
    id BIGSERIAL PRIMARY KEY,
    tracking_id TEXT NOT NULL,
    trans_ref_guid TEXT NOT NULL,
    policy_number TEXT NOT NULL,
    
    -- Patient Details
    patient_first_name TEXT NOT NULL,
    patient_last_name TEXT NOT NULL,
    patient_date_of_birth DATE NOT NULL,
    patient_government_id VARCHAR(50) NOT NULL,
    patient_street TEXT NOT NULL,
    patient_city TEXT NOT NULL,
    patient_state VARCHAR(2) NOT NULL,
    patient_zip VARCHAR(10) NOT NULL,
    patient_phone VARCHAR(20) NOT NULL,
    patient_email TEXT, -- Nullable if optional
    
    -- Doctor Details
    doctor_first_name TEXT NOT NULL,
    doctor_last_name TEXT NOT NULL,
    doctor_facility TEXT NOT NULL,
    doctor_street TEXT NOT NULL,
    doctor_city TEXT NOT NULL,
    doctor_state VARCHAR(2) NOT NULL,
    doctor_zip VARCHAR(10) NOT NULL,
    doctor_phone VARCHAR(20) NOT NULL,
    
    -- Metadata & Status
    copy_instructions TEXT,
    policy_amount NUMERIC(18, 2) NOT NULL,
    is_error BOOLEAN NOT NULL DEFAULT FALSE,
    error_message TEXT, -- Nullable when is_error is FALSE
    source VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);