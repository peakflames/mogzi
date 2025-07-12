# Test Procedure TP_01: Session Management Manual Testing

## Overview
This test procedure validates the session management functionality of Mogzi TUI application from a user perspective. It focuses on interactive chat interface usage, slash commands, and CLI arguments for session handling.

✅ **STATUS**: All core session management features are implemented and should pass these tests.

## Prerequisites
- Mogzi TUI executable built and available (e.g., `dist/mogzi`)
- Clean test environment (backup and remove `~/.mogzi/chats/` if it exists)
- Terminal access

## Test Environment Setup

### Setup Steps
1. **Backup existing sessions** (if any):
   ```bash
   mv ~/.mogzi/chats ~/.mogzi/chats.backup.$(date +%Y%m%d_%H%M%S) 2>/dev/null || true
   ```

2. **Verify clean state**:
   ```bash
   ls -la ~/.mogzi/chats/ 2>/dev/null || echo "No existing sessions (expected)"
   ```

3. **Set executable path** (adjust as needed):
   ```bash
   export MOGZI_EXE="./dist/mogzi"
   # or use full path: export MOGZI_EXE="/path/to/your/mogzi/executable"
   ```

## Test Cases

### TC_01: Default Session Creation and Basic Chat
**Objective**: Verify that a new session is automatically created and chat works normally.

**Requirements Traced**: TOR-5.3.1, TOR-5.3.7, TOR-5.3.15

**Steps**:
1. **Start Mogzi without any arguments**:
   ```bash
   $MOGZI_EXE
   ```

2. **Interact with the chat interface**:
   - Type: `Hello! This is my first message in a new session.`
   - Press Enter to send
   - Wait for AI response
   - Type: `Can you tell me what 2+2 equals?`
   - Press Enter and wait for response
   - Type: `Thank you for the calculation!`
   - Press Enter and wait for response

3. **Exit the application**:
   - Type: `/quit`
   - Press Enter

4. **Verification**:
   ```bash
   # Check that session file was created
   ls -la ~/.mogzi/chats/
   
   # Verify session contains our messages
   cat ~/.mogzi/chats/*.json | jq '.History | length'
   ```

**Expected Results**:
- ✅ Application starts with welcome message
- ✅ Chat interface responds to user messages
- ✅ Session file created in `~/.mogzi/chats/`
- ✅ Session file contains all exchanged messages

---

### TC_02: Session Persistence Verification
**Objective**: Verify that messages are immediately saved and persist across application restarts.

**Requirements Traced**: TOR-5.3, TOR-8.2

**Steps**:
1. **Start Mogzi**:
   ```bash
   $MOGZI_EXE
   ```

2. **Send a few messages**:
   - Type: `This is message 1 for persistence testing`
   - Press Enter, wait for response
   - Type: `This is message 2 for persistence testing`
   - Press Enter, wait for response

3. **Exit without using quit command** (simulate crash):
   - Press `Ctrl+C` to force exit

4. **Restart Mogzi**:
   ```bash
   $MOGZI_EXE
   ```

5. **Verify history is restored**:
   - Scroll up in the chat interface to see previous messages
   - Verify both test messages and AI responses are visible

6. **Add another message**:
   - Type: `This message was added after restart`
   - Press Enter, wait for response

7. **Exit properly**:
   - Type: `/quit`

**Expected Results**:
- ✅ Previous messages visible after restart
- ✅ New messages can be added to restored session
- ✅ No data loss from forced exit

---

### TC_03: Session Listing with Slash Command
**Objective**: Verify that sessions can be listed using the `/session list` command.

**Requirements Traced**: TOR-5.3.2, TOR-5.3.3

**Steps**:
1. **Create multiple sessions by starting and chatting**:
   
   **Session 1**:
   ```bash
   $MOGZI_EXE
   ```
   - Type: `Hello, this is session 1 for testing`
   - Wait for response
   - Type: `/quit`

   **Session 2**:
   ```bash
   $MOGZI_EXE
   ```
   - Type: `Hello, this is session 2 for testing`
   - Wait for response
   - Type: `/quit`

   **Session 3**:
   ```bash
   $MOGZI_EXE
   ```
   - Type: `Hello, this is session 3 for testing`
   - Wait for response
   - Type: `/quit`

2. **Start Mogzi and list sessions**:
   ```bash
   $MOGZI_EXE
   ```

3. **Use the session list command**:
   - Type: `/session list`
   - Press Enter

4. **Review the output**:
   - Note the session IDs displayed
   - Check creation dates and times
   - Verify message counts are shown

5. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Shows list of all 3+ sessions
- ✅ Displays session IDs, creation dates, and message counts
- ✅ Information is formatted clearly and readable

---

### TC_04: Loading Specific Session by ID
**Objective**: Verify that existing sessions can be loaded using the `--session` CLI argument.

**Requirements Traced**: TOR-5.3.4

**Steps**:
1. **Get a session ID from the previous test**:
   ```bash
   # List session files and pick one
   ls ~/.mogzi/chats/
   # Copy one of the session IDs (filename without .json)
   SESSION_ID="<paste-session-id-here>"
   ```

2. **Start Mogzi with specific session**:
   ```bash
   $MOGZI_EXE --session $SESSION_ID
   ```

3. **Verify the correct session loaded**:
   - Scroll up to see the chat history
   - Confirm you see the messages from that specific session
   - Note the session context in the interface

4. **Add a new message to verify it's the same session**:
   - Type: `This is a new message added to the loaded session`
   - Press Enter, wait for response

5. **Exit and verify persistence**:
   - Type: `/quit`
   - Restart with same session ID:
     ```bash
     $MOGZI_EXE --session $SESSION_ID
     ```
   - Verify the new message is still there

6. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Correct session history loads
- ✅ New messages are added to the same session
- ✅ Session persistence works correctly

---

### TC_05: Session Clearing
**Objective**: Verify that current session can be cleared using `/session clear`.

**Requirements Traced**: TOR-5.3.5

**Steps**:
1. **Start with an existing session that has history**:
   ```bash
   SESSION_ID="<use-session-id-from-previous-test>"
   $MOGZI_EXE --session $SESSION_ID
   ```

2. **Verify history is present**:
   - Scroll up to confirm previous messages are visible

3. **Clear the session**:
   - Type: `/session clear`
   - Press Enter

4. **Verify session is cleared**:
   - Check that chat history area is now empty/clean
   - Only welcome message should be visible

5. **Add a new message**:
   - Type: `This is the first message after clearing the session`
   - Press Enter, wait for response

6. **Exit and reload to verify**:
   - Type: `/quit`
   - Restart with same session:
     ```bash
     $MOGZI_EXE --session $SESSION_ID
     ```
   - Verify only the new message (and its response) are present

7. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Session history is completely cleared
- ✅ Only new messages after clear are retained
- ✅ Session file is properly updated

---

### TC_06: Session Renaming
**Objective**: Verify that sessions can be renamed using `/session rename`.

**Requirements Traced**: TOR-5.3.10

**Steps**:
1. **Start with an existing session**:
   ```bash
   SESSION_ID="<use-session-id-from-previous-test>"
   $MOGZI_EXE --session $SESSION_ID
   ```

2. **Rename the session**:
   - Type: `/session rename "My Important Test Session"`
   - Press Enter

3. **Verify rename confirmation**:
   - Check for success message in the interface

4. **Exit and verify rename persisted**:
   - Type: `/quit`
   - Start a new session:
     ```bash
     $MOGZI_EXE
     ```
   - Type: `/session list`
   - Look for the renamed session in the list

5. **Load the renamed session**:
   - Type: `/quit`
   - Load by ID:
     ```bash
     $MOGZI_EXE --session $SESSION_ID
     ```
   - Verify the session loads correctly with its new name

6. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Session is renamed successfully
- ✅ New name appears in session list
- ✅ Session can still be loaded by ID
- ✅ Rename persists across application restarts

---

### TC_07: Help and Command Discovery
**Objective**: Verify that users can discover session management commands through help.

**Requirements Traced**: TOR-2.3

**Steps**:
1. **Start Mogzi**:
   ```bash
   $MOGZI_EXE
   ```

2. **Check general help**:
   - Type: `/help`
   - Press Enter
   - Look for session-related commands

3. **Check session-specific help** (if available):
   - Type: `/session help` or `/session`
   - Press Enter

4. **Test command completion/hints** (if available):
   - Type: `/session ` (with space) and see if autocomplete shows options

5. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Help system shows available session commands
- ✅ Session commands are documented clearly
- ✅ Command syntax is explained

---

### TC_09: CLI Session List Command
**Objective**: Verify that sessions can be listed using the `mogzi session list` CLI command.

**Requirements Traced**: TOR-5.3.2, TOR-5.3.3

**Steps**:
1. **Ensure multiple sessions exist** (from previous tests):
   ```bash
   # Should have sessions from previous test cases
   ls ~/.mogzi/chats/
   ```

2. **Test CLI session list command**:
   ```bash
   $MOGZI_EXE session list
   ```

3. **Review the output**:
   - Verify table format with columns: Name, ID, Created, Last Modified, Initial Prompt
   - Check that session limit is respected (default: 10 most recent)
   - Verify contextual header shows "Available Chat Sessions (last X recently used)"
   - Note wider column formatting for better readability

**Expected Results**:
- ✅ Shows table of available sessions
- ✅ Displays all session metadata clearly
- ✅ Respects sessionListLimit configuration
- ✅ Table formatting is readable with appropriate column widths

---

### TC_10: CLI Session Info Command
**Objective**: Verify that detailed session information can be retrieved using `mogzi session info`.

**Requirements Traced**: TOR-5.3.2, TOR-5.3.3

**Steps**:
1. **Get a session name and ID from previous tests**:
   ```bash
   # List sessions to get names and IDs
   $MOGZI_EXE session list
   ```

2. **Test session info by name**:
   ```bash
   $MOGZI_EXE session info "session-name-here"
   ```

3. **Test session info by full GUID**:
   ```bash
   $MOGZI_EXE session info "full-session-guid-here"
   ```

4. **Test session info by partial GUID**:
   ```bash
   # Use last 12 characters of a session ID
   $MOGZI_EXE session info "partial-guid-here"
   ```

5. **Test invalid session**:
   ```bash
   $MOGZI_EXE session info "nonexistent-session"
   ```

**Expected Results**:
- ✅ Shows detailed session information for valid lookups
- ✅ Supports exact name matching
- ✅ Supports full GUID matching
- ✅ Supports partial GUID matching (EndsWith)
- ✅ Shows helpful error message for invalid sessions
- ✅ Displays usage instructions for starting chat with the session

---

### TC_08: Error Handling and Edge Cases
**Objective**: Verify graceful handling of invalid session operations.

**Requirements Traced**: TOR-5.3.6, TOR-8.2

**Steps**:
1. **Test invalid session ID**:
   ```bash
   $MOGZI_EXE --session "nonexistent-session-id"
   ```
   - Verify graceful error handling or new session creation

2. **Test invalid rename**:
   ```bash
   $MOGZI_EXE
   ```
   - Type: `/session rename ""`  (empty name)
   - Verify appropriate error message

3. **Test clearing empty session**:
   - Type: `/session clear` on a new/empty session
   - Verify no errors occur

4. **Exit**:
   - Type: `/quit`

**Expected Results**:
- ✅ Invalid operations show helpful error messages
- ✅ Application doesn't crash on invalid input
- ✅ User is guided on correct usage

---

## Test Completion Checklist

After completing all test cases, verify:

- [ ] Sessions are created automatically
- [ ] Messages persist immediately and across restarts
- [ ] Session listing works via slash command (`/session list`)
- [ ] Session listing works via CLI command (`mogzi session list`)
- [ ] Session info retrieval works via CLI command (`mogzi session info`)
- [ ] Sessions can be loaded by ID via CLI argument (`--session`)
- [ ] Session clearing removes all history (`/session clear`)
- [ ] Session renaming works and persists (`/session rename`)
- [ ] Help system provides adequate guidance
- [ ] Error handling is user-friendly for all commands
- [ ] CLI commands support flexible session lookup (name/GUID/partial)
- [ ] Table formatting is readable with appropriate column widths
- [ ] Session limit configuration is respected across all interfaces
- [ ] No data corruption or loss occurred
- [ ] All session files in `~/.mogzi/chats/` are valid JSON

## Cleanup

After testing, you may want to clean up test sessions:

```bash
# Remove test sessions
rm -rf ~/.mogzi/chats/

# Restore original sessions if backed up
mv ~/.mogzi/chats.backup.* ~/.mogzi/chats 2>/dev/null || true
```

## Notes

- Record any unexpected behavior or UI issues
- Note performance with multiple sessions
- Document any confusing command syntax or help text
- Test on your target platform (the published executable environment)
