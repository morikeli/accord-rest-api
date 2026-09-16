from rest_framework.response import Response
from rest_framework import status
from rest_framework.views import APIView


class HealthCheck(APIView):
    def get(self, request, *args, **kwargs):
        data = {"message": "OK! App ran successfully!"}
        return Response(data, status.HTTP_200_OK)