package com.eshop.catalog.config;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.web.servlet.ModelAndView;

@Component
public class SessionInfoInterceptor implements HandlerInterceptor {

  private static final String MACHINE_NAME_KEY = "machineName";
  private static final String SESSION_START_TIME_KEY = "sessionStartTime";
  private static final DateTimeFormatter FORMATTER =
      DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

  private final String machineName;

  public SessionInfoInterceptor() {
    this.machineName = resolveMachineName();
  }

  @Override
  public void postHandle(
      HttpServletRequest request,
      HttpServletResponse response,
      Object handler,
      ModelAndView modelAndView) {
    if (modelAndView == null) {
      return;
    }
    String viewName = modelAndView.getViewName();
    if (viewName != null && viewName.startsWith("redirect:")) {
      return;
    }

    HttpSession session = request.getSession(true);

    if (session.getAttribute(MACHINE_NAME_KEY) == null) {
      session.setAttribute(MACHINE_NAME_KEY, machineName);
    }
    if (session.getAttribute(SESSION_START_TIME_KEY) == null) {
      session.setAttribute(SESSION_START_TIME_KEY, LocalDateTime.now().format(FORMATTER));
    }

    modelAndView.getModel().put(MACHINE_NAME_KEY, session.getAttribute(MACHINE_NAME_KEY));
    modelAndView
        .getModel()
        .put(SESSION_START_TIME_KEY, session.getAttribute(SESSION_START_TIME_KEY));
  }

  private static String resolveMachineName() {
    String envHost = System.getenv("COMPUTERNAME");
    if (envHost != null && !envHost.isBlank()) {
      return envHost;
    }
    envHost = System.getenv("HOSTNAME");
    if (envHost != null && !envHost.isBlank()) {
      return envHost;
    }
    try {
      return InetAddress.getLocalHost().getHostName();
    } catch (UnknownHostException e) {
      return "unknown";
    }
  }
}
