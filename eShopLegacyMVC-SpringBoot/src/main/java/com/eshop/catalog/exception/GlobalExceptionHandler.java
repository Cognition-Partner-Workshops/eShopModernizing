package com.eshop.catalog.exception;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.server.ResponseStatusException;

import jakarta.persistence.EntityNotFoundException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

@ControllerAdvice
public class GlobalExceptionHandler {

    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    @ExceptionHandler(EntityNotFoundException.class)
    @ResponseStatus(HttpStatus.NOT_FOUND)
    public String handleEntityNotFound(EntityNotFoundException ex, HttpServletRequest request, Model model) {
        log.warn("Entity not found: {}", ex.getMessage());
        model.addAttribute("path", request.getRequestURI());
        return "error/404";
    }

    @ExceptionHandler(ResponseStatusException.class)
    public String handleResponseStatus(ResponseStatusException ex, HttpServletRequest request,
                                       HttpServletResponse response, Model model) {
        log.warn("Response status exception: {} {}", ex.getStatusCode(), ex.getReason());
        response.setStatus(ex.getStatusCode().value());
        if (ex.getStatusCode().value() == HttpStatus.NOT_FOUND.value()) {
            model.addAttribute("path", request.getRequestURI());
            return "error/404";
        }
        model.addAttribute("status", ex.getStatusCode().value());
        model.addAttribute("error", ex.getReason());
        return "error";
    }

    @ExceptionHandler(Exception.class)
    @ResponseStatus(HttpStatus.INTERNAL_SERVER_ERROR)
    public String handleGenericException(Exception ex, Model model) {
        log.error("Unexpected error", ex);
        model.addAttribute("status", HttpStatus.INTERNAL_SERVER_ERROR.value());
        model.addAttribute("error", "An unexpected error occurred");
        return "error";
    }
}
