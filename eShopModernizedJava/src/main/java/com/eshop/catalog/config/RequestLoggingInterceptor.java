package com.eshop.catalog.config;

import io.micrometer.tracing.Tracer;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;

import java.util.UUID;

@Component
public class RequestLoggingInterceptor implements HandlerInterceptor {

    private static final Logger log = LoggerFactory.getLogger(RequestLoggingInterceptor.class);

    private final Tracer tracer;

    public RequestLoggingInterceptor(Tracer tracer) {
        this.tracer = tracer;
    }

    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response,
                             Object handler) {
        String activityId = (tracer.currentSpan() != null && tracer.currentSpan().context() != null)
                ? tracer.currentSpan().context().traceId()
                : UUID.randomUUID().toString();

        MDC.put("activityId", activityId);
        MDC.put("requestInfo", request.getRequestURI() + ", " +
                (request.getHeader("User-Agent") != null ? request.getHeader("User-Agent") : ""));

        log.debug("WebApplication_BeginRequest");
        return true;
    }

    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response,
                                Object handler, Exception ex) {
        MDC.remove("activityId");
        MDC.remove("requestInfo");
    }
}
