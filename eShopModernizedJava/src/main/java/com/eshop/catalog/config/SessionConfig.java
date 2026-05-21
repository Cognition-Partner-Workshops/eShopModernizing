package com.eshop.catalog.config;

import jakarta.servlet.http.HttpSessionEvent;
import jakarta.servlet.http.HttpSessionListener;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.net.InetAddress;
import java.net.UnknownHostException;
import java.time.LocalDateTime;

@Configuration
public class SessionConfig {

    @Bean
    public HttpSessionListener httpSessionListener() {
        return new HttpSessionListener() {
            @Override
            public void sessionCreated(HttpSessionEvent se) {
                String machineName;
                try {
                    machineName = InetAddress.getLocalHost().getHostName();
                } catch (UnknownHostException e) {
                    machineName = "unknown";
                }
                se.getSession().setAttribute("MachineName", machineName);
                se.getSession().setAttribute("SessionStartTime", LocalDateTime.now().toString());
            }
        };
    }
}
