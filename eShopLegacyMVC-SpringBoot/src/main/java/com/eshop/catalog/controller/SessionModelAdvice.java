package com.eshop.catalog.controller;

import com.eshop.catalog.filter.SessionTrackingFilter;
import jakarta.servlet.http.HttpSession;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ModelAttribute;

/**
 * Exposes session-tracked machine name and session start time
 * as model attributes for the Thymeleaf layout footer.
 */
@ControllerAdvice
public class SessionModelAdvice {

    private static final DateTimeFormatter FORMATTER =
            DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

    @ModelAttribute("machineName")
    public String machineName(HttpSession session) {
        Object value = session.getAttribute(SessionTrackingFilter.MACHINE_NAME_ATTR);
        return value != null ? value.toString() : "localhost";
    }

    @ModelAttribute("sessionStartTime")
    public String sessionStartTime(HttpSession session) {
        Object value = session.getAttribute(SessionTrackingFilter.SESSION_START_TIME_ATTR);
        if (value instanceof LocalDateTime dateTime) {
            return dateTime.format(FORMATTER);
        }
        return "";
    }
}
