package com.eshop.catalog.filter;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;
import java.io.IOException;
import java.net.InetAddress;
import java.time.LocalDateTime;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Populates session attributes for machine name and session start time
 * on the first request of each HTTP session.
 */
public class SessionTrackingFilter extends OncePerRequestFilter {

    public static final String MACHINE_NAME_ATTR = "MachineName";
    public static final String SESSION_START_TIME_ATTR = "SessionStartTime";

    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                    HttpServletResponse response,
                                    FilterChain filterChain) throws ServletException, IOException {

        HttpSession session = request.getSession(true);

        if (session.isNew()) {
            session.setAttribute(MACHINE_NAME_ATTR, resolveHostName());
            session.setAttribute(SESSION_START_TIME_ATTR, LocalDateTime.now());
        }

        filterChain.doFilter(request, response);
    }

    private String resolveHostName() {
        try {
            return InetAddress.getLocalHost().getHostName();
        } catch (Exception e) {
            return "unknown";
        }
    }
}
