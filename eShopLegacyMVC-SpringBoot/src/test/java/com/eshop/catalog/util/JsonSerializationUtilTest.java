package com.eshop.catalog.util;

import org.junit.jupiter.api.Test;

import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class JsonSerializationUtilTest {

    record SampleItem(int id, String name, double price) {}

    @Test
    void serializeAndDeserializeBytes_roundTrip() {
        var original = new SampleItem(1, "Widget", 9.99);

        byte[] json = JsonSerializationUtil.serializeToJson(original);
        SampleItem restored = JsonSerializationUtil.deserializeFromJson(json, SampleItem.class);

        assertEquals(original, restored);
    }

    @Test
    void serializeAndDeserializeStream_roundTrip() {
        var original = new SampleItem(42, "Gadget", 19.95);

        InputStream stream = JsonSerializationUtil.serializeToJsonStream(original);
        SampleItem restored = JsonSerializationUtil.deserializeFromJson(stream, SampleItem.class);

        assertEquals(original, restored);
    }

    @Test
    void serializeToJson_producesValidJsonBytes() {
        var item = new SampleItem(7, "Test Item", 3.50);

        byte[] json = JsonSerializationUtil.serializeToJson(item);
        String jsonStr = new String(json);

        assertTrue(jsonStr.contains("\"name\":\"Test Item\""));
        assertTrue(jsonStr.contains("\"id\":7"));
        assertTrue(jsonStr.contains("\"price\":3.5"));
    }

    @Test
    void deserializeFromJson_withByteArray_handlesComplexObject() {
        record Nested(String label, List<Integer> values) {}
        var original = new Nested("numbers", List.of(1, 2, 3));

        byte[] json = JsonSerializationUtil.serializeToJson(original);
        Nested restored = JsonSerializationUtil.deserializeFromJson(json, Nested.class);

        assertEquals(original.label(), restored.label());
        assertEquals(original.values(), restored.values());
    }

    @Test
    void deserializeFromJson_withInvalidBytes_throwsException() {
        byte[] badJson = "not valid json".getBytes();

        assertThrows(JsonSerializationException.class,
                () -> JsonSerializationUtil.deserializeFromJson(badJson, SampleItem.class));
    }

    @Test
    void deserializeFromJson_withInvalidStream_throwsException() {
        InputStream badStream = new ByteArrayInputStream("<<<>>>".getBytes());

        assertThrows(JsonSerializationException.class,
                () -> JsonSerializationUtil.deserializeFromJson(badStream, SampleItem.class));
    }

    @Test
    void serializeToJsonStream_isReadableAndResetAtBeginning() throws Exception {
        var item = new SampleItem(99, "Stream Test", 1.00);

        InputStream stream = JsonSerializationUtil.serializeToJsonStream(item);

        assertTrue(stream.available() > 0, "Stream should have bytes available from the start");
    }

    @Test
    void roundTrip_withNullFields() {
        record NullableItem(Integer id, String name) {}
        var original = new NullableItem(null, null);

        byte[] json = JsonSerializationUtil.serializeToJson(original);
        NullableItem restored = JsonSerializationUtil.deserializeFromJson(json, NullableItem.class);

        assertNull(restored.id());
        assertNull(restored.name());
    }
}
