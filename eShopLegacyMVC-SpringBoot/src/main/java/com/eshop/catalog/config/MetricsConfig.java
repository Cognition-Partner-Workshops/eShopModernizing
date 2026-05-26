package com.eshop.catalog.config;

import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

@Configuration
public class MetricsConfig {

    @Bean
    public OncePerRequestFilter requestCountFilter(MeterRegistry registry) {
        final Counter totalRequests = Counter.builder("http_requests_total")
                .description("Total HTTP requests received")
                .register(registry);

        return new OncePerRequestFilter() {
            @Override
            protected void doFilterInternal(HttpServletRequest request,
                                            HttpServletResponse response,
                                            FilterChain filterChain)
                    throws ServletException, IOException {
                totalRequests.increment();
                filterChain.doFilter(request, response);
            }

            @Override
            protected boolean shouldNotFilter(HttpServletRequest request) {
                return request.getRequestURI().startsWith("/actuator");
            }
        };
    }
}
