package com.eshop.catalog.config;

import jakarta.servlet.http.HttpSessionEvent;
import jakarta.servlet.http.HttpSessionListener;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Ports the .NET {@code Session_Start} logic from Global.asax.cs. Stores machine hostname and
 * session start time in the HTTP session on creation for display in the layout footer.
 */
@Configuration
public class SessionConfig {

  private static final Logger log = LoggerFactory.getLogger(SessionConfig.class);

  public static final String MACHINE_NAME_KEY = "machineName";
  public static final String SESSION_START_TIME_KEY = "sessionStartTime";

  private static final DateTimeFormatter FORMATTER =
      DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

  @Bean
  public HttpSessionListener sessionTrackingListener() {
    String machineName = resolveMachineName();
    log.info("Session tracking configured — machineName={}", machineName);

    return new HttpSessionListener() {
      @Override
      public void sessionCreated(HttpSessionEvent event) {
        event.getSession().setAttribute(MACHINE_NAME_KEY, machineName);
        event
            .getSession()
            .setAttribute(SESSION_START_TIME_KEY, LocalDateTime.now().format(FORMATTER));
      }
    };
  }

  static String resolveMachineName() {
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
