package com.eshop.catalog.exception;

import jakarta.servlet.http.HttpServletRequest;
import java.time.Instant;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class GlobalExceptionHandler {

  private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

  @ExceptionHandler(Exception.class)
  public Object handleException(Exception ex, HttpServletRequest request) {
    log.error("Unhandled exception at {}: {}", request.getRequestURI(), ex.getMessage(), ex);

    if (isApiRequest(request)) {
      Map<String, Object> body =
          Map.of(
              "timestamp", Instant.now().toString(),
              "status", HttpStatus.INTERNAL_SERVER_ERROR.value(),
              "error", HttpStatus.INTERNAL_SERVER_ERROR.getReasonPhrase(),
              "message", ex.getMessage() != null ? ex.getMessage() : "Unexpected error",
              "path", request.getRequestURI());
      return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(body);
    }

    return "error";
  }

  @ExceptionHandler(IllegalArgumentException.class)
  public Object handleIllegalArgument(IllegalArgumentException ex, HttpServletRequest request) {
    log.warn("Bad request at {}: {}", request.getRequestURI(), ex.getMessage());

    if (isApiRequest(request)) {
      Map<String, Object> body =
          Map.of(
              "timestamp", Instant.now().toString(),
              "status", HttpStatus.BAD_REQUEST.value(),
              "error", HttpStatus.BAD_REQUEST.getReasonPhrase(),
              "message", ex.getMessage() != null ? ex.getMessage() : "Bad request",
              "path", request.getRequestURI());
      return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(body);
    }

    return "error";
  }

  private boolean isApiRequest(HttpServletRequest request) {
    String path = request.getRequestURI();
    return path.startsWith("/api/");
  }
}
