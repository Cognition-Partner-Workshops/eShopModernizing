package com.eshop.catalog.util;

import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;

/**
 * Jackson-based JSON serialization utility, replacing the legacy BinaryFormatter-based
 * eShopLegacy.Utilities.Serializing class.
 */
public final class JsonSerializationUtil {

  private static final ObjectMapper OBJECT_MAPPER = new ObjectMapper();

  private JsonSerializationUtil() {}

  public static InputStream serializeToStream(Object value) throws IOException {
    ByteArrayOutputStream out = new ByteArrayOutputStream();
    OBJECT_MAPPER.writeValue(out, value);
    return new ByteArrayInputStream(out.toByteArray());
  }

  public static <T> T deserializeFromStream(InputStream stream, Class<T> type) throws IOException {
    return OBJECT_MAPPER.readValue(stream, type);
  }
}
