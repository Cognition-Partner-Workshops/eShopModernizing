package com.eshop.catalog.config;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.jdbc.core.JdbcTemplate;

@ExtendWith(MockitoExtension.class)
class CatalogItemHiLoGeneratorTest {

  @Mock private JdbcTemplate jdbcTemplate;

  private CatalogItemHiLoGenerator generator;

  @BeforeEach
  void setUp() {
    generator = new CatalogItemHiLoGenerator(jdbcTemplate);
  }

  @Test
  void firstCallFetchesFromSequence() {
    when(jdbcTemplate.queryForObject(anyString(), eq(Long.class))).thenReturn(1L);

    int id = generator.getNextSequenceValue();

    assertThat(id).isEqualTo(1);
    verify(jdbcTemplate, times(1)).queryForObject(anyString(), eq(Long.class));
  }

  @Test
  void subsequentCallsUseLocalIncrement() {
    when(jdbcTemplate.queryForObject(anyString(), eq(Long.class))).thenReturn(1L);

    int first = generator.getNextSequenceValue();
    int second = generator.getNextSequenceValue();
    int third = generator.getNextSequenceValue();

    assertThat(first).isEqualTo(1);
    assertThat(second).isEqualTo(2);
    assertThat(third).isEqualTo(3);
    verify(jdbcTemplate, times(1)).queryForObject(anyString(), eq(Long.class));
  }

  @Test
  void fetchesNewSequenceValueAfterIncrementExhausted() {
    when(jdbcTemplate.queryForObject(anyString(), eq(Long.class))).thenReturn(1L).thenReturn(11L);

    for (int i = 0; i < 10; i++) {
      generator.getNextSequenceValue();
    }

    int eleventhId = generator.getNextSequenceValue();

    assertThat(eleventhId).isEqualTo(11);
    verify(jdbcTemplate, times(2)).queryForObject(anyString(), eq(Long.class));
  }

  @Test
  void generatesCorrectSequenceOfTenIds() {
    when(jdbcTemplate.queryForObject(anyString(), eq(Long.class))).thenReturn(1L);

    for (int i = 1; i <= 10; i++) {
      assertThat(generator.getNextSequenceValue()).isEqualTo(i);
    }

    verify(jdbcTemplate, times(1)).queryForObject(anyString(), eq(Long.class));
  }

  @Test
  void threadSafety() throws InterruptedException {
    when(jdbcTemplate.queryForObject(anyString(), eq(Long.class)))
        .thenReturn(1L)
        .thenReturn(11L)
        .thenReturn(21L)
        .thenReturn(31L)
        .thenReturn(41L);

    java.util.Set<Integer> ids = java.util.concurrent.ConcurrentHashMap.newKeySet();
    int threadCount = 5;
    int idsPerThread = 10;
    java.util.concurrent.CountDownLatch latch =
        new java.util.concurrent.CountDownLatch(threadCount);

    for (int t = 0; t < threadCount; t++) {
      new Thread(
              () -> {
                try {
                  for (int i = 0; i < idsPerThread; i++) {
                    ids.add(generator.getNextSequenceValue());
                  }
                } finally {
                  latch.countDown();
                }
              })
          .start();
    }

    latch.await();

    assertThat(ids).hasSize(threadCount * idsPerThread);
  }
}
