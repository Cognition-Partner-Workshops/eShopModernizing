package com.eshop.catalog.config;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;

@Component
public class HiLoSequenceGenerator {

    private static final int HI_LO_INCREMENT = 10;

    private final JdbcTemplate jdbcTemplate;
    private final Map<String, SequenceState> sequences = new ConcurrentHashMap<>();

    public HiLoSequenceGenerator(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    public int getNextValue(String sequenceName) {
        if (!sequenceName.matches("[a-zA-Z_][a-zA-Z0-9_]*")) {
            throw new IllegalArgumentException("Invalid sequence name: " + sequenceName);
        }
        SequenceState state = sequences.computeIfAbsent(sequenceName, k -> new SequenceState());
        synchronized (state) {
            if (state.remainingLoIds == 0) {
                Long nextVal = jdbcTemplate.queryForObject(
                        "SELECT NEXT VALUE FOR " + sequenceName, Long.class);
                if (nextVal == null) {
                    throw new IllegalStateException(sequenceName + " sequence returned null");
                }
                state.currentId = nextVal.intValue();
                state.remainingLoIds = HI_LO_INCREMENT - 1;
                return state.currentId;
            } else {
                state.remainingLoIds--;
                return ++state.currentId;
            }
        }
    }

    private static class SequenceState {
        int currentId = -1;
        int remainingLoIds = 0;
    }
}
