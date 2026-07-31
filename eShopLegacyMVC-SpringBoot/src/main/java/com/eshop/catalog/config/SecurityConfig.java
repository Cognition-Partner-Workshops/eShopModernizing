package com.eshop.catalog.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.web.SecurityFilterChain;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

  @Bean
  public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
    http.authorizeHttpRequests(auth -> auth.anyRequest().permitAll())
        .csrf(csrf -> csrf.ignoringRequestMatchers("/api/**", "/h2-console/**", "/actuator/**"))
        .headers(headers -> headers.frameOptions(frame -> frame.sameOrigin()));
    return http.build();
  }
}
