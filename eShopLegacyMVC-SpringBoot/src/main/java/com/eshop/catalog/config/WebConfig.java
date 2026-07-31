package com.eshop.catalog.config;

import com.eshop.catalog.filter.SessionTrackingFilter;
import org.springframework.boot.web.servlet.FilterRegistrationBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class WebConfig {

    @Bean
    public FilterRegistrationBean<SessionTrackingFilter> sessionTrackingFilter() {
        var registration = new FilterRegistrationBean<>(new SessionTrackingFilter());
        registration.addUrlPatterns("/*");
        registration.setOrder(1);
        return registration;
    }
}
