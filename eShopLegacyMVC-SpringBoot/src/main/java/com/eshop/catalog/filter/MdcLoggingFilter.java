package com.eshop.catalog.filter;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Ports the log4net {@code ActivityIdHelper} and {@code WebRequestInfo} patterns from
 * Global.asax.cs {@code Application_BeginRequest} to SLF4J MDC. Sets per-request diagnostic context
 * so every log line includes a correlation ID, request URL, and user agent.
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class MdcLoggingFilter extends OncePerRequestFilter {

  private static final Logger log = LoggerFactory.getLogger(MdcLoggingFilter.class);

  private static final String MDC_ACTIVITY_ID = "activityId";
  private static final String MDC_REQUEST_INFO = "requestInfo";

  @Override
  protected void doFilterInternal(
      HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
      throws ServletException, IOException {
    try {
      String activityId = UUID.randomUUID().toString();
      String requestInfo = request.getRequestURI() + ", " + request.getHeader("User-Agent");

      MDC.put(MDC_ACTIVITY_ID, activityId);
      MDC.put(MDC_REQUEST_INFO, requestInfo);

      log.debug("WebApplication_BeginRequest");

      filterChain.doFilter(request, response);
    } finally {
      MDC.remove(MDC_ACTIVITY_ID);
      MDC.remove(MDC_REQUEST_INFO);
    }
  }
}
