package com.eshop.catalog.dto;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

class BrandDtoTest {

  private final ObjectMapper objectMapper = new ObjectMapper();

  @Test
  void shouldExposeIdAndBrandFields() {
    BrandDto dto = new BrandDto(1, "Azure");

    assertThat(dto.id()).isEqualTo(1);
    assertThat(dto.brand()).isEqualTo("Azure");
  }

  @Test
  void shouldSerializeToJson() throws Exception {
    BrandDto dto = new BrandDto(42, "Roslyn");

    String json = objectMapper.writeValueAsString(dto);

    assertThat(json).contains("\"id\":42");
    assertThat(json).contains("\"brand\":\"Roslyn\"");
  }

  @Test
  void shouldDeserializeFromJson() throws Exception {
    String json = "{\"id\":7,\"brand\":\"Contoso\"}";

    BrandDto dto = objectMapper.readValue(json, BrandDto.class);

    assertThat(dto.id()).isEqualTo(7);
    assertThat(dto.brand()).isEqualTo("Contoso");
  }

  @Test
  void shouldImplementEqualsAndHashCode() {
    BrandDto a = new BrandDto(1, "Azure");
    BrandDto b = new BrandDto(1, "Azure");

    assertThat(a).isEqualTo(b);
    assertThat(a.hashCode()).isEqualTo(b.hashCode());
  }

  @Test
  void shouldImplementToString() {
    BrandDto dto = new BrandDto(3, "Fabrikam");

    assertThat(dto.toString()).contains("3");
    assertThat(dto.toString()).contains("Fabrikam");
  }
}
