package com.eshop.catalog.util;

/**
 * Unchecked exception thrown when JSON serialization or deserialization fails.
 */
public class JsonSerializationException extends RuntimeException {

    public JsonSerializationException(String message, Throwable cause) {
        super(message, cause);
    }
}
