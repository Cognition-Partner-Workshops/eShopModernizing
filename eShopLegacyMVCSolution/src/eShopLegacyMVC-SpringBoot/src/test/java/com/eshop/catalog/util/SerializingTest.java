package com.eshop.catalog.util;

import com.eshop.catalog.dto.BrandDTO;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

class SerializingTest {

    private Serializing serializing;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @BeforeEach
    void setUp() {
        serializing = new Serializing();
    }

    @Test
    void serializeAndDeserializeSingleBrandDTO() {
        BrandDTO original = new BrandDTO(1, "Alpine");

        byte[] json = serializing.serializeToJson(original);
        assertNotNull(json);
        assertTrue(json.length > 0);

        BrandDTO restored = serializing.deserializeFromJson(json, BrandDTO.class);
        assertEquals(original, restored);
    }

    @Test
    void serializeAndDeserializeListOfBrandDTOs() throws Exception {
        List<BrandDTO> originals = List.of(
                new BrandDTO(1, "Alpine"),
                new BrandDTO(2, "Contoso"),
                new BrandDTO(3, "Northwind")
        );

        byte[] json = serializing.serializeToJson(originals);
        assertNotNull(json);

        List<BrandDTO> restored = objectMapper.readValue(json, new TypeReference<List<BrandDTO>>() {});
        assertEquals(originals.size(), restored.size());
        for (int i = 0; i < originals.size(); i++) {
            assertEquals(originals.get(i), restored.get(i));
        }
    }

    @Test
    void serializeAndDeserializeEmptyList() throws Exception {
        List<BrandDTO> emptyList = List.of();

        byte[] json = serializing.serializeToJson(emptyList);
        assertNotNull(json);
        assertArrayEquals("[]".getBytes(), json);

        List<BrandDTO> restored = objectMapper.readValue(json, new TypeReference<List<BrandDTO>>() {});
        assertTrue(restored.isEmpty());
    }

    @Test
    void deserializeInvalidJsonThrowsException() {
        byte[] invalid = "not json".getBytes();

        assertThrows(RuntimeException.class, () ->
                serializing.deserializeFromJson(invalid, BrandDTO.class));
    }
}
