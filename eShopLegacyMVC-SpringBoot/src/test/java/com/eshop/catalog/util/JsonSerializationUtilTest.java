package com.eshop.catalog.util;

import org.junit.jupiter.api.Test;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;

class JsonSerializationUtilTest {

    @Test
    void roundTripSerialization() throws IOException {
        SampleDto original = new SampleDto(42, "Test Brand");

        byte[] serialized = JsonSerializationUtil.serializeToStream(original);
        assertNotNull(serialized);

        InputStream stream = new ByteArrayInputStream(serialized);
        SampleDto deserialized = JsonSerializationUtil.deserializeFromStream(stream, SampleDto.class);

        assertEquals(original.id(), deserialized.id());
        assertEquals(original.brand(), deserialized.brand());
    }

    @Test
    void serializeNull() throws IOException {
        byte[] serialized = JsonSerializationUtil.serializeToStream(null);
        assertNotNull(serialized);
        assertEquals("null", new String(serialized));
    }

    @Test
    void deserializeInvalidJsonThrowsException() {
        InputStream badStream = new ByteArrayInputStream("not-json".getBytes());
        assertThrows(IOException.class,
                () -> JsonSerializationUtil.deserializeFromStream(badStream, SampleDto.class));
    }

    @Test
    void roundTripWithList() throws IOException {
        List<SampleDto> originals = List.of(
                new SampleDto(1, "Alpha"),
                new SampleDto(2, "Beta")
        );

        byte[] serialized = JsonSerializationUtil.serializeToStream(originals);
        assertNotNull(serialized);

        InputStream stream = new ByteArrayInputStream(serialized);
        SampleDto[] deserialized = JsonSerializationUtil.deserializeFromStream(stream, SampleDto[].class);

        assertEquals(2, deserialized.length);
        assertEquals("Alpha", deserialized[0].brand());
        assertEquals(2, deserialized[1].id());
    }

    record SampleDto(int id, String brand) {
    }
}
