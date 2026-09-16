from rest_framework import serializers

from .models import ApsIncoming


class HealthCheckSerializer(serializers.Serializer):
    message = serializers.CharField()


class AcordJSONIntakeSerializer(serializers.ModelSerializer):
    source = serializers.CharField(read_only=True)

    class Meta:
        model = ApsIncoming
        fields = '__all__'

    def to_internal_value(self, data):
        field_aliases = {
            "transRefGuid": "trans_ref_guid",
            "trackingId": "tracking_id",
            "policyNumber": "policy_number",
            "insuredFirstName": "patient_first_name",
            "insuredLastName": "patient_last_name",
            "insuredBirthDate": "patient_date_of_birth",
            "insuredGovernmentId": "patient_government_id",
            "insuredStreet": "patient_street",
            "insuredCity": "patient_city",
            "insuredState": "patient_state",
            "insuredZip": "patient_zip",
            "insuredPhone": "patient_phone",
            "insuredEmail": "patient_email",
            "physicianFirstName": "doctor_first_name",
            "physicianLastName": "doctor_last_name",
            "physicianFacility": "doctor_facility",
            "physicianStreet": "doctor_street",
            "physicianCity": "doctor_city",
            "physicianState": "doctor_state",
            "physicianZip": "doctor_zip",
            "physicianPhone": "doctor_phone",
            "requirementDetails": "copy_instructions",
            "faceAmount": "policy_amount",
        }
        normalized_data = data.copy()
        # Translate ACORD camelCase keys to the model's snake_case field names.
        for input_name, model_name in field_aliases.items():
            if input_name in normalized_data:
                normalized_data[model_name] = normalized_data.pop(input_name)

        return super().to_internal_value(normalized_data)
