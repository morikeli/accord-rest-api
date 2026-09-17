from django.contrib.auth.models import User
from django.contrib.auth.password_validation import validate_password
from rest_framework import serializers

from .models import ApsIncoming


class HealthCheckSerializer(serializers.Serializer):
    message = serializers.CharField()


class SignupSerializer(serializers.ModelSerializer):
    password = serializers.CharField(write_only=True, min_length=8)
    confirm_password = serializers.CharField(write_only=True, min_length=8)

    class Meta:
        model = User
        fields = [
            "username",
            "email",
            "password",
            "confirm_password",
        ]

    def validate(self, attrs):
        if attrs["password"] != attrs["confirm_password"]:
            raise serializers.ValidationError({"password": "Passwords do not match!"})
        validate_password(attrs["password"])
        return attrs

    def create(self, validated_data):
        validated_data.pop("password_confirm")

        return User.objects.create_user(**validated_data)


class AcordJSONIntakeSerializer(serializers.ModelSerializer):
    source = serializers.CharField(read_only=True)

    class Meta:
        model = ApsIncoming
        fields = '__all__'
        read_only_fields = ["id", "source", "created_at", "updated_at"]

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
