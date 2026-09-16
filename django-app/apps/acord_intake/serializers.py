from rest_framework import serializers

from .models import ApsIncoming


class HealthCheckSerializer(serializers.Serializer):
    message = serializers.CharField()


class AcordJSONIntakeSerializer(serializers.ModelSerializer):
    source = serializers.CharField(read_only=True)

    class Meta:
        model = ApsIncoming
        fields = '__all__'
