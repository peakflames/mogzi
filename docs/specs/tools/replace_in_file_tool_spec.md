# replace_in_file Tool Specification

## Purpose
Make targeted edits to specific parts of an existing file using SEARCH/REPLACE blocks. This is the preferred tool for making precise modifications without rewriting entire files.

## Parameters
- `path` (required): The file path to modify relative to the current working directory
- `diff` (required): One or more SEARCH/REPLACE blocks defining exact changes

## Usage
```xml
<replace_in_file>
<path>src/app.js</path>
<diff>
------- SEARCH
import React from 'react';
=======
import React, { useState } from 'react';
+++++++ REPLACE

------- SEARCH
function handleSubmit() {
  saveData();
}
=======
function handleSubmit() {
  saveData();
  setLoading(false);
}
+++++++ REPLACE
</diff>
</replace_in_file>
```

## Critical Rules
1. **Exact Matching**: SEARCH content must match the file section EXACTLY (character-for-character)
2. **Complete Lines**: Include complete lines, never partial lines or fragments
3. **First Match Only**: SEARCH/REPLACE blocks only replace the first match occurrence
4. **Order Matters**: List multiple blocks in the order they appear in the file
5. **Proper Format**: Use exact marker format without modifications

## SEARCH/REPLACE Block Format
```
------- SEARCH
[exact content to find]
=======
[new content to replace with]
+++++++ REPLACE
```

## Best Practices
- Default choice for most file modifications
- More efficient and safer than rewriting entire files
- Include just enough context lines for unique matching
- Break large changes into smaller, focused blocks
- Limit to <5 SEARCH/REPLACE blocks per operation for large files

## When to Use
- Making small, targeted changes
- Updating specific functions or methods
- Modifying configuration values
- Adding imports or dependencies
- Changing variable names or values
- Most code modifications and improvements

## When NOT to Use
- Creating new files (use `write_to_file`)
- Complete file restructuring
- When majority of file content needs to change

## Common Patterns

### Adding Code
```xml
------- SEARCH
function existingFunction() {
  return data;
}
=======
function existingFunction() {
  return data;
}

function newFunction() {
  return newData;
}
+++++++ REPLACE
```

### Deleting Code
```xml
------- SEARCH
function obsoleteFunction() {
  // old code
}

function keepThis() {
=======
function keepThis() {
+++++++ REPLACE
```

### Moving Code
Use two blocks: one to delete from original location, one to insert at new location.

## Error Handling
- Returns error if SEARCH content doesn't match exactly
- File reverts to original state on failure
- Provides detailed error messages for debugging
- Suggests using fewer, more precise SEARCH blocks

## Auto-formatting Considerations
- Editor may automatically format the file after changes
- Final file state may differ from input due to formatting rules
- Use the returned final_file_content as reference for subsequent edits
- SEARCH blocks must match the formatted content, not the original input

## Integration with Other Tools
- Often preceded by `read_file` to understand current content
- May be used multiple times for complex modifications
- Commonly combined with `search_files` to find modification targets
