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
    String hostname;
    try {
      hostname = InetAddress.getLocalHost().getHostName();
    } catch (UnknownHostException e) {
      hostname = "unknown";
    }
    this.machineName = hostname;
    this.sessionStartTime =
        LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss"));
  }

  @ModelAttribute("sessionInfo")
  public String sessionInfo() {
    return machineName + ", " + sessionStartTime;
  }
}
