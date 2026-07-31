package com.eshop.catalog.filter;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.core.Ordered;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.UUID;

/**
 * Servlet filter that populates MDC with a unique activity ID and request info
 * for each incoming request. Ported from .NET Global.asax.cs ActivityIdHelper
 * and WebRequestInfo.
 */
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class RequestLoggingFilter extends OncePerRequestFilter {

    private static final Logger log = LoggerFactory.getLogger(RequestLoggingFilter.class);

    private static final String MDC_ACTIVITY_ID = "activityId";
    private static final String MDC_REQUEST_INFO = "requestInfo";

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain) throws ServletException, IOException {
        try {
            String activityId = UUID.randomUUID().toString();
            String requestInfo = buildRequestInfo(request);

            MDC.put(MDC_ACTIVITY_ID, activityId);
            MDC.put(MDC_REQUEST_INFO, requestInfo);

            log.debug("BeginRequest");

            filterChain.doFilter(request, response);
        } finally {
            MDC.remove(MDC_ACTIVITY_ID);
            MDC.remove(MDC_REQUEST_INFO);
        }
    }

    private String buildRequestInfo(HttpServletRequest request) {
        String url = request.getRequestURI();
        String queryString = request.getQueryString();
        if (queryString != null) {
            url += "?" + queryString;
        }
        String userAgent = request.getHeader("User-Agent");
        return url + ", " + (userAgent != null ? userAgent : "");
    }
}
