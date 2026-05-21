package com.eshop.catalog.config;

import jakarta.servlet.http.HttpSession;
import jakarta.servlet.http.HttpSessionEvent;
import jakarta.servlet.http.HttpSessionListener;
import org.springframework.stereotype.Component;

@Component
public class SessionListener implements HttpSessionListener {
    @Override
    public void sessionCreated(HttpSessionEvent event) {
        HttpSession session = event.getSession();
        try {
            session.setAttribute("MachineName", java.net.InetAddress.getLocalHost().getHostName());
        } catch (Exception e) {
            session.setAttribute("MachineName", "unknown");
        }
        session.setAttribute("SessionStartTime", java.time.LocalDateTime.now().toString());
    }
}
