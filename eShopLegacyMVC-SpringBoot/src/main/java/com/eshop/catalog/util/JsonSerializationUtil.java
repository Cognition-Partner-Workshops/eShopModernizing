package com.eshop.catalog.util;

import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;

/**
 * JSON serialization utility replacing legacy BinaryFormatter-based serialization.
 * Uses Jackson ObjectMapper for safe, interoperable JSON serialization.
 */
public final class JsonSerializationUtil {

    private static final ObjectMapper objectMapper = new ObjectMapper();

    private JsonSerializationUtil() {
        // Utility class — prevent instantiation
    }

    /**
     * Serializes an object to a JSON byte array.
     *
     * @param input the object to serialize
     * @return JSON representation as byte[]
     * @throws JsonSerializationException if serialization fails
     */
    public static byte[] serializeToJson(Object input) {
        try {
            return objectMapper.writeValueAsBytes(input);
        } catch (IOException e) {
            throw new JsonSerializationException("Failed to serialize object to JSON", e);
        }
    }

    /**
     * Serializes an object to a JSON InputStream.
     *
     * @param input the object to serialize
     * @return JSON representation as InputStream (positioned at the beginning)
     * @throws JsonSerializationException if serialization fails
     */
    public static InputStream serializeToJsonStream(Object input) {
        return new ByteArrayInputStream(serializeToJson(input));
    }

    /**
     * Deserializes a JSON byte array into an object of the specified type.
     *
     * @param <T>   the target type
     * @param json  the JSON byte array
     * @param clazz the target class
     * @return the deserialized object
     * @throws JsonSerializationException if deserialization fails
     */
    public static <T> T deserializeFromJson(byte[] json, Class<T> clazz) {
        try {
            return objectMapper.readValue(json, clazz);
        } catch (IOException e) {
            throw new JsonSerializationException("Failed to deserialize JSON to " + clazz.getName(), e);
        }
    }

    /**
     * Deserializes a JSON InputStream into an object of the specified type.
     *
     * @param <T>    the target type
     * @param stream the JSON InputStream
     * @param clazz  the target class
     * @return the deserialized object
     * @throws JsonSerializationException if deserialization fails
     */
    public static <T> T deserializeFromJson(InputStream stream, Class<T> clazz) {
        try {
            return objectMapper.readValue(stream, clazz);
        } catch (IOException e) {
            throw new JsonSerializationException("Failed to deserialize JSON stream to " + clazz.getName(), e);
        }
    }
}
