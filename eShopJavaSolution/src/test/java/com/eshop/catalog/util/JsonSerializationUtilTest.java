package com.eshop.catalog.util;

import static org.assertj.core.api.Assertions.assertThat;

import java.io.IOException;
import java.io.InputStream;
import org.junit.jupiter.api.Test;

class JsonSerializationUtilTest {

  @Test
  void roundTripSingleObject() throws IOException {
    SampleDto original = new SampleDto(1, "Test Brand");

    InputStream stream = JsonSerializationUtil.serializeToStream(original);
    SampleDto result = JsonSerializationUtil.deserializeFromStream(stream, SampleDto.class);

    assertThat(result.id()).isEqualTo(original.id());
    assertThat(result.name()).isEqualTo(original.name());
  }

  @Test
  void roundTripArray() throws IOException {
    SampleDto[] original = {new SampleDto(1, "Alpha"), new SampleDto(2, "Beta")};

    InputStream stream = JsonSerializationUtil.serializeToStream(original);
    SampleDto[] result = JsonSerializationUtil.deserializeFromStream(stream, SampleDto[].class);

    assertThat(result).hasSize(2);
    assertThat(result[0].id()).isEqualTo(1);
    assertThat(result[0].name()).isEqualTo("Alpha");
    assertThat(result[1].id()).isEqualTo(2);
    assertThat(result[1].name()).isEqualTo("Beta");
  }

  @Test
  void roundTripEmptyObject() throws IOException {
    SampleDto original = new SampleDto(0, "");

    InputStream stream = JsonSerializationUtil.serializeToStream(original);
    SampleDto result = JsonSerializationUtil.deserializeFromStream(stream, SampleDto.class);

    assertThat(result.id()).isZero();
    assertThat(result.name()).isEmpty();
  }

  record SampleDto(int id, String name) {}
}
