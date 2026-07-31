# ---- Build Stage ----
FROM eclipse-temurin:21-jdk AS build

WORKDIR /app

# Copy Maven wrapper and POM first for dependency caching
COPY mvnw mvnw.cmd pom.xml ./
COPY .mvn .mvn

RUN chmod +x mvnw && \
    ./mvnw dependency:go-offline -B

# Copy source code
COPY src src

# Build the application, skipping tests (tests run in CI)
RUN ./mvnw package -DskipTests -B && \
    mv target/*.jar target/app.jar

# ---- Runtime Stage ----
FROM eclipse-temurin:21-jre-alpine

RUN apk add --no-cache curl && \
    addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app

COPY --from=build /app/target/app.jar app.jar

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/actuator/health || exit 1

ENTRYPOINT ["java", "-jar", "app.jar"]
