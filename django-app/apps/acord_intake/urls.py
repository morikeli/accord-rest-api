from django.urls import path
from . import views

urlpatterns = [
    path("health", views.HealthCheckView.as_view()),
    path("intake/json", views.IntakeAPIView.as_view()),
    
]