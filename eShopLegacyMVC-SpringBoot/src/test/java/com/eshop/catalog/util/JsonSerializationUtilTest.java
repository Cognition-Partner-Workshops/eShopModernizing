package com.eshop.catalog.util;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.eshop.catalog.util.JsonSerializationUtil.SerializationException;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Objects;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class JsonSerializationUtilTest {

  private JsonSerializationUtil serializer;

  @BeforeEach
  void setUp() {
    serializer = new JsonSerializationUtil();
  }

  @Test
  void serializeToJson_simpleObject_returnsValidJson() {
    TestProduct product = new TestProduct(1, "Widget", new BigDecimal("19.99"));

    String json = serializer.serializeToJson(product);

    assertThat(json).contains("\"id\":1");
    assertThat(json).contains("\"name\":\"Widget\"");
    assertThat(json).contains("\"price\":19.99");
  }

  @Test
  void deserializeFromJson_validJson_returnsObject() {
    String json = "{\"id\":1,\"name\":\"Widget\",\"price\":19.99}";

    TestProduct product = serializer.deserializeFromJson(json, TestProduct.class);

    assertThat(product.getId()).isEqualTo(1);
    assertThat(product.getName()).isEqualTo("Widget");
    assertThat(product.getPrice()).isEqualByComparingTo(new BigDecimal("19.99"));
  }

  @Test
  void serializeAndDeserializeJson_roundTrip_preservesData() {
    TestProduct original = new TestProduct(42, "Gadget", new BigDecimal("99.95"));

    String json = serializer.serializeToJson(original);
    TestProduct restored = serializer.deserializeFromJson(json, TestProduct.class);

    assertThat(restored).isEqualTo(original);
  }

  @Test
  void serializeToBytes_simpleObject_returnsNonEmptyBytes() {
    TestProduct product = new TestProduct(1, "Widget", new BigDecimal("19.99"));

    byte[] bytes = serializer.serializeToBytes(product);

    assertThat(bytes).isNotEmpty();
  }

  @Test
  void deserializeFromBytes_validBytes_returnsObject() {
    TestProduct original = new TestProduct(7, "Bolt", new BigDecimal("0.50"));
    byte[] bytes = serializer.serializeToBytes(original);

    TestProduct restored = serializer.deserializeFromBytes(bytes, TestProduct.class);

    assertThat(restored).isEqualTo(original);
  }

  @Test
  void serializeAndDeserializeBytes_roundTrip_preservesData() {
    TestProduct original = new TestProduct(99, "Spring", new BigDecimal("3.14"));

    byte[] bytes = serializer.serializeToBytes(original);
    TestProduct restored = serializer.deserializeFromBytes(bytes, TestProduct.class);

    assertThat(restored).isEqualTo(original);
  }

  @Test
  void serializeToJson_withLocalDateTime_handlesJavaTime() {
    TestEvent event = new TestEvent("launch", LocalDateTime.of(2025, 6, 15, 10, 30, 0));

    String json = serializer.serializeToJson(event);
    TestEvent restored = serializer.deserializeFromJson(json, TestEvent.class);

    assertThat(restored.getName()).isEqualTo("launch");
    assertThat(restored.getTimestamp()).isEqualTo(LocalDateTime.of(2025, 6, 15, 10, 30, 0));
  }

  @Test
  void serializeToBytes_withLocalDateTime_handlesJavaTime() {
    TestEvent event = new TestEvent("deploy", LocalDateTime.of(2025, 12, 25, 0, 0, 0));

    byte[] bytes = serializer.serializeToBytes(event);
    TestEvent restored = serializer.deserializeFromBytes(bytes, TestEvent.class);

    assertThat(restored.getName()).isEqualTo("deploy");
    assertThat(restored.getTimestamp()).isEqualTo(LocalDateTime.of(2025, 12, 25, 0, 0, 0));
  }

  @Test
  void deserializeFromJson_invalidJson_throwsSerializationException() {
    assertThatThrownBy(() -> serializer.deserializeFromJson("not valid json", TestProduct.class))
        .isInstanceOf(SerializationException.class)
        .hasMessageContaining("Failed to deserialize JSON to TestProduct");
  }

  @Test
  void deserializeFromBytes_invalidBytes_throwsSerializationException() {
    byte[] garbage = new byte[] {0x00, 0x01, 0x02};

    assertThatThrownBy(() -> serializer.deserializeFromBytes(garbage, TestProduct.class))
        .isInstanceOf(SerializationException.class)
        .hasMessageContaining("Failed to deserialize bytes to TestProduct");
  }

  @Test
  void serializeToJson_nullFields_includesNulls() {
    TestProduct product = new TestProduct(1, null, null);

    String json = serializer.serializeToJson(product);

    assertThat(json).contains("\"name\":null");
    assertThat(json).contains("\"price\":null");
  }

  @Test
  void serializeToJson_collection_returnsJsonArray() {
    List<TestProduct> products =
        List.of(
            new TestProduct(1, "A", new BigDecimal("1.00")),
            new TestProduct(2, "B", new BigDecimal("2.00")));

    String json = serializer.serializeToJson(products);

    assertThat(json).startsWith("[");
    assertThat(json).endsWith("]");
    assertThat(json).contains("\"name\":\"A\"");
    assertThat(json).contains("\"name\":\"B\"");
  }

  static class TestProduct {
    private int id;
    private String name;
    private BigDecimal price;

    TestProduct() {}

    TestProduct(int id, String name, BigDecimal price) {
      this.id = id;
      this.name = name;
      this.price = price;
    }

    public int getId() {
      return id;
    }

    public void setId(int id) {
      this.id = id;
    }

    public String getName() {
      return name;
    }

    public void setName(String name) {
      this.name = name;
    }

    public BigDecimal getPrice() {
      return price;
    }

    public void setPrice(BigDecimal price) {
      this.price = price;
    }

    @Override
    public boolean equals(Object o) {
      if (this == o) return true;
      if (o == null || getClass() != o.getClass()) return false;
      TestProduct that = (TestProduct) o;
      return id == that.id && Objects.equals(name, that.name) && Objects.equals(price, that.price);
    }

    @Override
    public int hashCode() {
      return Objects.hash(id, name, price);
    }
  }

  static class TestEvent {
    private String name;
    private LocalDateTime timestamp;

    TestEvent() {}

    TestEvent(String name, LocalDateTime timestamp) {
      this.name = name;
      this.timestamp = timestamp;
    }

    public String getName() {
      return name;
    }

    public void setName(String name) {
      this.name = name;
    }

    public LocalDateTime getTimestamp() {
      return timestamp;
    }

    public void setTimestamp(LocalDateTime timestamp) {
      this.timestamp = timestamp;
    }
  }
}
