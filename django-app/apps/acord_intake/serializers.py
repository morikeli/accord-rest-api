from rest_framework import serializers

from .models import ApsIncoming

class AcordJSONIntakeSerializer(serializers.ModelSerializer):
    source = serializers.CharField(read_only=True)

    class Meta:
        model = ApsIncoming
        fields = '__all__'
