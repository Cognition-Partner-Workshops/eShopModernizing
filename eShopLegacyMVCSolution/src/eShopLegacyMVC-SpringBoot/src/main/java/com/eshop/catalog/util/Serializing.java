package com.eshop.catalog.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;

public class Serializing {

    private static final ObjectMapper objectMapper = new ObjectMapper();

    public byte[] serializeToJson(Object input) {
        try {
            return objectMapper.writeValueAsBytes(input);
        } catch (JsonProcessingException e) {
            throw new RuntimeException("Serialization failed", e);
        }
    }

    public <T> T deserializeFromJson(byte[] data, Class<T> clazz) {
        try {
            return objectMapper.readValue(data, clazz);
        } catch (Exception e) {
            throw new RuntimeException("Deserialization failed", e);
        }
    }
}
