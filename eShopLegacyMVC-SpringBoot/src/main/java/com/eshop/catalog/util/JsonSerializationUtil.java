package com.eshop.catalog.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.io.InputStream;

/**
 * JSON serialization utility replacing legacy .NET BinaryFormatter.
 * Uses Jackson ObjectMapper for safe, portable serialization.
 */
public final class JsonSerializationUtil {

    private static final ObjectMapper objectMapper = new ObjectMapper();

    private JsonSerializationUtil() {
    }

    /**
     * Serializes an object to a JSON byte array.
     *
     * @param input the object to serialize
     * @return JSON-encoded byte array
     * @throws JsonProcessingException if serialization fails
     */
    public static byte[] serializeToStream(Object input) throws JsonProcessingException {
        return objectMapper.writeValueAsBytes(input);
    }

    /**
     * Deserializes a JSON input stream into an object of the specified type.
     *
     * @param stream the input stream containing JSON data
     * @param type   the target class
     * @param <T>    the target type
     * @return the deserialized object
     * @throws IOException if deserialization fails
     */
    public static <T> T deserializeFromStream(InputStream stream, Class<T> type) throws IOException {
        return objectMapper.readValue(stream, type);
    }
}
