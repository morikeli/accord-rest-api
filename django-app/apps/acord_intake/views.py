from rest_framework.response import Response
from rest_framework import status
from rest_framework.views import APIView

from .serializers import HealthCheckSerializer


@extend_schema(tags=["/"], summary="Check app status")
class HealthCheckView(GenericAPIView):
    """
    Health check endpoint to verify that the application is running and responsive.
    """

    permission_classes = [permissions.AllowAny]
    serializer_class = HealthCheckSerializer

    def get(self, request, *args, **kwargs):
        serializer = self.get_serializer(
            instance={"message": "OK! App ran successfully!"}
        )
        return Response(serializer.data, status.HTTP_200_OK)

