from rest_framework.response import Response

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


@extend_schema(tags=["Auth"], summary="Create a user account")
class SignupView(CreateAPIView):
    """Create an active user account."""

    permission_classes = [permissions.AllowAny]
    serializer_class = SignupSerializer


@extend_schema(tags=["Intake"], summary="Parse APS JSON response")
class IntakeAPIView(CreateAPIView):
    """
    API endpoint for handling intake of APS records.
    """

    serializer_class = AcordJSONIntakeSerializer

    @extend_schema(responses={201: AcordJSONIntakeSerializer})
    def perform_create(self, serializer):
        """Save the validated intake record and identify its source as JSON."""
        serializer.save(source="json")


@extend_schema_view(
    list=extend_schema(summary="Retrieve and list all APS records"),
    retrieve=extend_schema(summary="Retrieve a single APS record"),
    update=extend_schema(summary="Update an APS record"),
    destroy=extend_schema(summary="Delete an APS record"),
)
@extend_schema(tags=["APS"])
class APSRecordsViewSet(viewsets.ModelViewSet):
    """
    API endpoint for listing, retrieving, updating, and deleting APS records.
    """

    queryset = ApsIncoming.objects.all().order_by("-created_at")
    serializer_class = AcordJSONIntakeSerializer
    pagination_class = StandardResultsSetPagination
    http_method_names = ["get", "put", "delete", "head", "options"]

    def get_object(self):
        # Let the ViewSet look up the record using the URL's primary-key value.
        try:
            return super().get_object()
        except Http404:
            # Convert Django's lookup exception into a DRF 404 response with a
            # clearer message for API clients.
            raise exceptions.NotFound("The requested record could not be found!")
