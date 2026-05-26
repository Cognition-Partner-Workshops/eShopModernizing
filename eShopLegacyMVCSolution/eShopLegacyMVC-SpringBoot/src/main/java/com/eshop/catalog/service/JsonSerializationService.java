package com.eshop.catalog.service;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;

import org.springframework.stereotype.Service;

/**
 * JSON serialization service replacing the legacy .NET BinaryFormatter utility.
 */
@Service
public class JsonSerializationService {

    private final ObjectMapper objectMapper;

    public JsonSerializationService() {
        this.objectMapper = new ObjectMapper();
        this.objectMapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        this.objectMapper.configure(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS, false);
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

    public static class SerializationException extends RuntimeException {

        public SerializationException(String message, Throwable cause) {
            super(message, cause);
        }
    }
}
