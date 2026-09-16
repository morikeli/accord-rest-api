from django.contrib import admin
from django.urls import path, include
from drf_spectacular.views import (
    SpectacularAPIView,
    SpectacularRedocView,
    SpectacularSwaggerView,
)

urlpatterns = [
    path("api/", include("acord_intake.urls")),
    path("api/auth/token", DecoratedTokenObtainView.as_view(), name="token_obtain_pair"),
    path("api/auth/token/refresh", DecoratedTokenRefreshView.as_view(), name="token_refresh"),
    path("admin/", admin.site.urls),

    # Schema generator
    path("api/schema/", SpectacularAPIView.as_view(), name="schema"),
    # Swagger UI    
    path(
        "api/docs/",
        SpectacularSwaggerView.as_view(url_name="schema"),
        name="swagger-ui",
    ),
    # Redoc UI
    path(
        "api/redoc/",
        SpectacularRedocView.as_view(url_name="schema"),
        name="redoc",
    ),
]
