from rest_framework.pagination import PageNumberPagination


class StandardResultsSetPagination(PageNumberPagination):
    page_size = 10  # default no. of items returned per page
    # Allows API clients to dynamically request custom page sizes by appending a query parameter to the URL.
    page_size_query_param = "page_size"
    max_page_size = 100
