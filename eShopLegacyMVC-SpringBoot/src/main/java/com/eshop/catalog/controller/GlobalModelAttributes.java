package com.eshop.catalog.controller;

import com.eshop.catalog.config.SessionConfig;
import jakarta.servlet.http.HttpSession;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ModelAttribute;

/**
 * Exposes session-tracked values (machine name, session start time) as Thymeleaf model attributes
 * for the layout footer. Values are populated by {@link SessionConfig}'s {@code
 * HttpSessionListener} on session creation.
 */
@ControllerAdvice
public class GlobalModelAttributes {

  @ModelAttribute("machineName")
  public String machineName(HttpSession session) {
    Object value = session.getAttribute(SessionConfig.MACHINE_NAME_KEY);
    return value != null ? value.toString() : "unknown";
  }

  @ModelAttribute("sessionStartTime")
  public String sessionStartTime(HttpSession session) {
    Object value = session.getAttribute(SessionConfig.SESSION_START_TIME_KEY);
    return value != null ? value.toString() : "";
  }

  @ModelAttribute("sessionInfo")
  public String sessionInfo(HttpSession session) {
    return machineName(session) + ", " + sessionStartTime(session);
  }
}
