# Use the official Python image that matches the project version.
FROM python:3.14-slim

# Prevent Python from writing .pyc files and enable unbuffered output for logs.
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1

# Set the container working directory.
WORKDIR /app

# Install uv so dependencies can be managed from pyproject.toml instead of requirements.txt.
RUN pip install --no-cache-dir uv

# Copy only dependency metadata first so Docker can cache dependency installation.
COPY pyproject.toml ./
COPY uv.lock* ./

# Install dependencies without installing the project itself yet.
RUN uv sync --frozen --no-install-project

# Copy the Django source code into the container.
COPY . .

# Install the project and final dependency set after the app code is in place.
RUN uv sync --frozen

# Expose the internal Django port used by the dev server.
EXPOSE 8000

# Start Django on all interfaces so it can be reached from Docker Compose or the host machine.
CMD ["uv", "run", "python", "manage.py", "runserver", "0.0.0.0:8000"]
