from django.urls import path
from . import views

urlpatterns = [
    path("health", views.HealthCheckView.as_view()),
    path("auth/signup", views.SignupView.as_view()),
    path("aps", views.APSRecordsViewSet.as_view({"post": "create"})),
    path("aps/all", views.APSRecordsViewSet.as_view({"get": "list"})),
    path(
        "aps/<int:pk>",
        views.APSRecordsViewSet.as_view(
            {"get": "retrieve", "put": "update", "delete": "destroy"}
        ),
    ),
]