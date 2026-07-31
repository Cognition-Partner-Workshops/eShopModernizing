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
import org.springframework.web.servlet.ModelAndView;
import org.springframework.web.servlet.resource.NoResourceFoundException;

@ControllerAdvice
public class GlobalExceptionHandler {

  private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

  @ExceptionHandler(ResourceNotFoundException.class)
  public Object handleResourceNotFound(ResourceNotFoundException ex, HttpServletRequest request) {
    log.warn("Resource not found at {}: {}", request.getRequestURI(), ex.getMessage());

    if (isApiRequest(request)) {
      return ResponseEntity.status(HttpStatus.NOT_FOUND)
          .body(errorBody(HttpStatus.NOT_FOUND, ex.getMessage(), request));
    }

    return errorView(HttpStatus.NOT_FOUND, ex.getMessage());
  }

  @ExceptionHandler(NoResourceFoundException.class)
  public Object handleNoResourceFound(NoResourceFoundException ex, HttpServletRequest request) {
    log.warn("No resource found at {}: {}", request.getRequestURI(), ex.getMessage());

    if (isApiRequest(request)) {
      return ResponseEntity.status(HttpStatus.NOT_FOUND)
          .body(errorBody(HttpStatus.NOT_FOUND, "The requested resource was not found", request));
    }

    return errorView(HttpStatus.NOT_FOUND, "The requested page was not found.");
  }

  @ExceptionHandler(IllegalArgumentException.class)
  public Object handleIllegalArgument(IllegalArgumentException ex, HttpServletRequest request) {
    log.warn("Bad request at {}: {}", request.getRequestURI(), ex.getMessage());

    if (isApiRequest(request)) {
      return ResponseEntity.status(HttpStatus.BAD_REQUEST)
          .body(
              errorBody(
                  HttpStatus.BAD_REQUEST,
                  ex.getMessage() != null ? ex.getMessage() : "Bad request",
                  request));
    }

    return errorView(
        HttpStatus.BAD_REQUEST, ex.getMessage() != null ? ex.getMessage() : "Bad request");
  }

  @ExceptionHandler(Exception.class)
  public Object handleException(Exception ex, HttpServletRequest request) {
    log.error("Unhandled exception at {}: {}", request.getRequestURI(), ex.getMessage(), ex);

    if (isApiRequest(request)) {
      return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
          .body(
              errorBody(
                  HttpStatus.INTERNAL_SERVER_ERROR,
                  ex.getMessage() != null ? ex.getMessage() : "Unexpected error",
                  request));
    }

    return errorView(
        HttpStatus.INTERNAL_SERVER_ERROR, "An error occurred while processing your request.");
  }

  private boolean isApiRequest(HttpServletRequest request) {
    String path = request.getRequestURI();
    return path.startsWith("/api/");
  }

  private Map<String, Object> errorBody(
      HttpStatus status, String message, HttpServletRequest request) {
    return Map.of(
        "timestamp", Instant.now().toString(),
        "status", status.value(),
        "error", status.getReasonPhrase(),
        "message", message,
        "path", request.getRequestURI());
  }

  private ModelAndView errorView(HttpStatus status, String message) {
    ModelAndView mav = new ModelAndView("error");
    mav.addObject("status", status.value());
    mav.addObject("message", message);
    mav.setStatus(status);
    return mav;
  }
}
