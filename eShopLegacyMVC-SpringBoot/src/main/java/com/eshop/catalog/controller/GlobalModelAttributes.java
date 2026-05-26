package com.eshop.catalog.controller;

import java.net.InetAddress;
import java.net.UnknownHostException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ModelAttribute;

@ControllerAdvice
public class GlobalModelAttributes {

  private final String machineName;
  private final String sessionStartTime;

  public GlobalModelAttributes() {
    this.machineName = resolveMachineName();
    this.sessionStartTime =
        LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss"));
  }

  @ModelAttribute("machineName")
  public String machineName() {
    return machineName;
  }

  @ModelAttribute("sessionStartTime")
  public String sessionStartTime() {
    return sessionStartTime;
  }

  @ModelAttribute("sessionInfo")
  public String sessionInfo() {
    return machineName + ", " + sessionStartTime;
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
