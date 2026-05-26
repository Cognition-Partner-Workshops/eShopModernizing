package com.eshop.catalog.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@Configuration
public class WebMvcConfig implements WebMvcConfigurer {

  private final SessionInfoInterceptor sessionInfoInterceptor;

  public WebMvcConfig(SessionInfoInterceptor sessionInfoInterceptor) {
    this.sessionInfoInterceptor = sessionInfoInterceptor;
  }

  @Override
  public void addInterceptors(InterceptorRegistry registry) {
    registry.addInterceptor(sessionInfoInterceptor);
  }
}
