package com.eshop.catalog.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import java.io.IOException;
import org.springframework.stereotype.Component;

@Component
public class JsonSerializationUtil {

  private final ObjectMapper objectMapper;

  public JsonSerializationUtil() {
    this.objectMapper = new ObjectMapper();
    this.objectMapper.registerModule(new JavaTimeModule());
  }

  public String serializeToJson(Object input) {
    try {
      return objectMapper.writeValueAsString(input);
    } catch (JsonProcessingException e) {
      throw new SerializationException("Failed to serialize object to JSON", e);
    }
  }

  public <T> T deserializeFromJson(String json, Class<T> clazz) {
    try {
      return objectMapper.readValue(json, clazz);
    } catch (JsonProcessingException e) {
      throw new SerializationException("Failed to deserialize JSON to " + clazz.getSimpleName(), e);
    }
  }

  public byte[] serializeToBytes(Object input) {
    try {
      return objectMapper.writeValueAsBytes(input);
    } catch (JsonProcessingException e) {
      throw new SerializationException("Failed to serialize object to bytes", e);
    }
  }

  public <T> T deserializeFromBytes(byte[] bytes, Class<T> clazz) {
    try {
      return objectMapper.readValue(bytes, clazz);
    } catch (IOException e) {
      throw new SerializationException(
          "Failed to deserialize bytes to " + clazz.getSimpleName(), e);
    }
  }

  public static class SerializationException extends RuntimeException {
    public SerializationException(String message, Throwable cause) {
      super(message, cause);
    }
  }
}
