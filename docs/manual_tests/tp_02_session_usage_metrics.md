# Manual Test Plan: Session Usage Metrics Feature

## Overview
This test plan covers the manual ad hoc testing of the session usage metrics feature implemented following the Cline-inspired design. The feature tracks input tokens, output tokens, and request count per session, displaying them in the footer with Cline-style formatting.

## Test Environment Setup
- **Build Status**: ✅ Confirmed working (no compilation errors)
- **Test Location**: `/home/todd/peakflames/mogzi`
- **Prerequisites**: Valid AI model configuration (OpenAI, Gemini, Claude, etc.)
- **Expected Footer Format**: `[Tokens: ↑ 1.9k ↓ 345] [Cache: --] [Context: 78.9k/200.0k (39%)]`

## Test Categories

### 1. Basic Functionality Testing

#### Test 1.1: Initial State Verification
**Objective**: Verify footer displays correctly with no session or empty session
**Steps**:
1. Start Mogzi: `cd src && dotnet run --project Mogzi.TUI`
2. Observe footer display before creating any session
3. Create new session: `/session new test-session`
4. Observe footer display with empty session

**Expected Results**:
- No session: Footer shows `session: no session` and `Tokens: --`
- New session: Footer shows `session: test-session` and `Tokens: ↑ 0 ↓ 0` or `Tokens: --`

#### Test 1.2: First AI Interaction
**Objective**: Verify token counting starts working after first AI message
**Steps**:
1. Send simple message: `Hello, can you help me?`
2. Wait for AI response to complete
3. Observe footer token display updates

**Expected Results**:
- Footer shows `Tokens: ↑ [input_count] ↓ [output_count]` with non-zero values
- Numbers should be formatted according to Cline style (e.g., `↑ 1.2k ↓ 345`)

#### Test 1.3: Cumulative Token Tracking
**Objective**: Verify tokens accumulate across multiple messages
**Steps**:
1. Note current token counts from Test 1.2
2. Send another message: `Can you write a simple Python function?`
3. Wait for response
4. Verify token counts increased

**Expected Results**:
- Input tokens should be higher than previous test
- Output tokens should be higher than previous test
- Values should represent cumulative totals, not per-message

### 2. Number Formatting Testing

#### Test 2.1: Small Numbers (< 1000)
**Objective**: Verify small token counts display without abbreviation
**Steps**:
1. Start fresh session
2. Send very short message: `Hi`
3. Observe token formatting

**Expected Results**:
- Numbers under 1000 should display as-is (e.g., `↑ 123 ↓ 45`)

#### Test 2.2: Thousands Range (1k - 999k)
**Objective**: Verify thousand-range formatting
**Steps**:
1. Continue conversation to accumulate tokens in thousands range
2. Send multiple messages or request longer responses
3. Observe formatting transitions

**Expected Results**:
- 1000-9999: Format as `1.0k` to `9.9k` (one decimal)
- 10000+: Format as `10k`, `15k`, etc. (no decimal)

#### Test 2.3: Millions Range (1m+)
**Objective**: Verify million-range formatting (if achievable)
**Steps**:
1. This may require very long conversations or large context
2. Observe if formatting switches to `1.0m` format

**Expected Results**:
- Numbers ≥ 1,000,000 should format as `1.0m`, `2.5m`, etc.

### 3. Context Window Testing

#### Test 3.1: Green Zone (0-50%)
**Objective**: Verify green color coding for low context usage
**Steps**:
1. Start fresh session
2. Send short messages to stay under 50% context
3. Observe context window display color

**Expected Results**:
- Context display should be green: `[green]Context: 12.3k/200.0k (6%)[/]`
- Percentage should be accurate

#### Test 3.2: Yellow Zone (51-80%)
**Objective**: Verify yellow color coding for medium context usage
**Steps**:
1. Send longer messages or attach files to increase context
2. Monitor context percentage in footer
3. Observe color change at 51%

**Expected Results**:
- Context display should turn yellow when >50%
- Format: `[yellow]Context: 120.5k/200.0k (60%)[/]`

#### Test 3.3: Red Zone (81-100%)
**Objective**: Verify red color coding for high context usage
**Steps**:
1. Continue adding context to reach >80%
2. Observe color change to red

**Expected Results**:
- Context display should turn red when >80%
- Format: `[red]Context: 170.0k/200.0k (85%)[/]`

### 4. Session Persistence Testing

#### Test 4.1: Session Save/Load
**Objective**: Verify usage metrics persist across session saves/loads
**Steps**:
1. Build up token usage in a session
2. Note current token counts
3. Save session: `/session save`
4. Exit Mogzi
5. Restart Mogzi and load session: `/session load [session-name]`
6. Verify token counts match

**Expected Results**:
- Token counts should be identical after reload
- Session file should contain usage metrics in JSON

#### Test 4.2: Session File Inspection
**Objective**: Verify usage metrics are properly serialized
**Steps**:
1. After building up usage, save session
2. Locate session file in `~/.mogzi/sessions/`
3. Examine JSON content for usage metrics

**Expected Results**:
- JSON should contain `usageMetrics` object
- Should have `inputTokens`, `outputTokens`, `requestCount`, `lastUpdated`

### 5. Multi-Session Testing

#### Test 5.1: Session Isolation
**Objective**: Verify usage metrics are session-scoped
**Steps**:
1. Create session A with some token usage
2. Create session B: `/session new session-b`
3. Verify session B starts with zero tokens
4. Switch back to session A: `/session load session-a`
5. Verify session A retains its token counts

**Expected Results**:
- Each session maintains independent token counts
- Switching sessions updates footer display correctly

#### Test 5.2: Multiple Session Comparison
**Objective**: Compare usage across different sessions
**Steps**:
1. Create multiple sessions with different usage patterns
2. Switch between them and observe footer changes
3. Verify each maintains its own metrics

**Expected Results**:
- Footer updates immediately when switching sessions
- Each session's metrics are preserved independently

### 6. Error Handling Testing

#### Test 6.1: No Session State
**Objective**: Verify graceful handling when no session exists
**Steps**:
1. Start Mogzi without creating session
2. Observe footer display
3. Try to send message (if possible)

**Expected Results**:
- Footer should show `Tokens: --` or similar placeholder
- No crashes or exceptions

#### Test 6.2: Corrupted Session Data
**Objective**: Verify handling of invalid usage metrics
**Steps**:
1. Create session with usage data
2. Manually edit session file to corrupt usage metrics
3. Load session and observe behavior

**Expected Results**:
- Should handle gracefully, possibly resetting metrics
- No application crashes

### 7. Visual Layout Testing

#### Test 7.1: Terminal Width Variations
**Objective**: Verify footer layout adapts to different terminal widths
**Steps**:
1. Start Mogzi in normal terminal
2. Resize terminal to very narrow width
3. Resize to very wide width
4. Observe footer layout behavior

**Expected Results**:
- Footer should adapt gracefully to width changes
- Information should remain readable
- No text truncation issues

#### Test 7.2: Long Session Names
**Objective**: Verify footer handles long session names
**Steps**:
1. Create session with very long name
2. Observe footer layout with long session name

**Expected Results**:
- Footer should handle long names gracefully
- May truncate session name if needed to fit other info

### 8. Model Compatibility Testing

#### Test 8.1: Different AI Models
**Objective**: Verify usage tracking works across different models
**Steps**:
1. Test with OpenAI models (if configured)
2. Test with Gemini models (if configured)
3. Test with Claude models (if configured)
4. Verify context window calculations are model-appropriate

**Expected Results**:
- Usage tracking should work regardless of model
- Context window sizes should reflect model capabilities
- Token counting should be accurate per model

#### Test 8.2: Model Switching
**Objective**: Verify behavior when switching models mid-session
**Steps**:
1. Start conversation with one model
2. Switch to different model (if supported)
3. Continue conversation
4. Observe usage tracking continuity

**Expected Results**:
- Usage should continue accumulating
- Context window display should update for new model

### 9. Performance Testing

#### Test 9.1: High Token Volume
**Objective**: Verify performance with large token counts
**Steps**:
1. Generate high token usage through long conversations
2. Monitor application responsiveness
3. Verify formatting remains correct at high numbers

**Expected Results**:
- Application should remain responsive
- Number formatting should work correctly
- No performance degradation

#### Test 9.2: Rapid Message Sequence
**Objective**: Verify usage tracking with rapid messages
**Steps**:
1. Send multiple messages in quick succession
2. Verify all usage is tracked correctly
3. Check for race conditions or missed updates

**Expected Results**:
- All messages should be tracked
- Token counts should be accurate
- No lost or duplicate counting

## Test Execution Notes

### Before Testing
1. Ensure valid AI model configuration exists
2. Clear any existing sessions if needed: `rm -rf ~/.mogzi/sessions/*`
3. Check logs location: `~/.mogzi/logs/`

### During Testing
1. Monitor application logs for errors
2. Take screenshots of footer display for documentation
3. Note any unexpected behavior or crashes
4. Verify JSON session files are updated correctly

### After Testing
1. Document any issues found
2. Verify all test cases pass
3. Note any performance concerns
4. Recommend any improvements

## Success Criteria

The session usage metrics feature is considered successfully tested when:

1. ✅ Token counting works accurately for all AI interactions
2. ✅ Cline-style number formatting displays correctly (345, 1.9k, 15k, 1.9m)
3. ✅ Context window color coding works (green/yellow/red)
4. ✅ Session persistence maintains usage metrics across restarts
5. ✅ Multi-session isolation works correctly
6. ✅ Error handling is graceful for edge cases
7. ✅ Visual layout adapts to different terminal sizes
8. ✅ Performance remains acceptable with high usage
9. ✅ All AI models supported show accurate metrics

## Known Limitations

- Cache metrics display as `Cache: --` (future implementation)
- Context window sizes are estimated based on model patterns
- Very high token counts (>1B) may need additional formatting consideration
