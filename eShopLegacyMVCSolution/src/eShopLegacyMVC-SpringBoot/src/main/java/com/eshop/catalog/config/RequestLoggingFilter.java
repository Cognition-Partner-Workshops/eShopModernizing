package com.eshop.catalog.config;

import jakarta.servlet.Filter;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.ServletRequest;
import jakarta.servlet.ServletResponse;
import jakarta.servlet.http.HttpServletRequest;
import java.io.IOException;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;

@Component
public class RequestLoggingFilter implements Filter {
    private static final Logger log = LoggerFactory.getLogger(RequestLoggingFilter.class);

    @Override
    public void doFilter(ServletRequest request, ServletResponse response, FilterChain chain)
            throws IOException, ServletException {
        HttpServletRequest httpRequest = (HttpServletRequest) request;

        // Set MDC activityId (UUID for request correlation)
        String activityId = UUID.randomUUID().toString();
        MDC.put("activityId", activityId);

        // Set MDC requestInfo (URL + User-Agent)
        String requestInfo = httpRequest.getRequestURI() + ", " + httpRequest.getHeader("User-Agent");
        MDC.put("requestInfo", requestInfo);

        log.debug("WebApplication_BeginRequest");

        try {
            chain.doFilter(request, response);
        } finally {
            MDC.clear();
        }
    }
}
