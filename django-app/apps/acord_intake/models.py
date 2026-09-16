from django.db import models


class ApsIncoming(models.Model):
    id = models.BigAutoField(primary_key=True)
    tracking_id = models.TextField(db_index=True)
    trans_ref_guid = models.TextField(db_index=True)
    policy_number = models.TextField(db_index=True)

    # Patient Details
    patient_first_name = models.CharField(max_length=150, db_index=True)
    patient_last_name = models.CharField(max_length=150, db_index=True)
    patient_date_of_birth = models.DateField()
    patient_government_id = models.CharField(max_length=50)
    patient_street = models.TextField()
    patient_city = models.TextField()
    patient_state = models.CharField(max_length=2)
    patient_zip = models.CharField(max_length=10)
    patient_phone = models.CharField(max_length=20)
    patient_email = models.TextField(blank=True, null=True)

    # Doctor Details
    doctor_first_name = models.CharField(max_length=150, db_index=True)
    doctor_last_name = models.CharField(max_length=150, db_index=True)
    doctor_facility = models.TextField()
    doctor_street = models.TextField()
    doctor_city = models.TextField()
    doctor_state = models.CharField(max_length=2)
    doctor_zip = models.CharField(max_length=10)
    doctor_phone = models.CharField(max_length=20)

    # Metadata & Status
    copy_instructions = models.TextField(blank=True, null=True)
    policy_amount = models.DecimalField(max_digits=18, decimal_places=2)
    is_error = models.BooleanField(default=False)
    error_message = models.TextField(blank=True, null=True)
    source = models.CharField(max_length=50)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        managed = False
        db_table = "aps_incoming"
