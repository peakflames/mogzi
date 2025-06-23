# preview_patch_application Tool Specification

## Purpose
Preview what changes a patch would make without actually applying them. This tool allows validation and review of patches before committing to file modifications, providing a safe way to verify patch correctness.

## Parameters
- `path` (required): The path of the file the patch would be applied to
- `patch` (required): The unified diff patch to preview

## Usage
```xml
<preview_patch_application>
<path>src/config.js</path>
<patch>--- a/src/config.js
+++ b/src/config.js
@@ -1,3 +1,4 @@
-const API_URL = 'http://localhost:3000';
+const API_URL = 'https://api.production.com';
 const DEBUG = false;
+const VERSION = '1.0.0';
</patch>
</preview_patch_application>
```

## Capabilities
- Simulates patch application without modifying files
- Shows the resulting file content after patch application
- Validates patch format and applicability
- Provides detailed success/failure information
- Tests fuzzy matching capabilities
- Reports potential conflicts or issues

## Preview Output
The tool provides comprehensive preview information:
- **Success/Failure Status**: Whether the patch can be applied
- **Preview Content**: The complete file content after patch application
- **Change Statistics**: Lines added, removed, and modified
- **Fuzzy Matching Details**: If fuzzy matching would be required
- **Conflict Information**: Details about any application failures

## Best Practices
- Always preview complex patches before applying
- Use to validate patches from external sources
- Review preview content for correctness
- Check for unintended side effects
- Verify fuzzy matching behavior when needed

## When to Use
- Validating patches before application
- Testing patches from version control systems
- Reviewing generated patches for correctness
- Debugging patch application failures
- Ensuring patches work with current file state

## Preview Scenarios

### Successful Application
```xml
<preview_patch_application>
<path>src/utils.js</path>
<patch>--- a/src/utils.js
+++ b/src/utils.js
@@ -1,3 +1,6 @@
 function existingFunction() {
   return 'existing';
 }
+
+function newFunction() {
+  return 'new';
+}
</patch>
</preview_patch_application>
```

### Conflict Detection
```xml
<preview_patch_application>
<path>src/modified.js</path>
<patch>--- a/src/modified.js
+++ b/src/modified.js
@@ -1,3 +1,3 @@
-const OLD_VALUE = 'old';
+const NEW_VALUE = 'new';
 const OTHER = 'unchanged';
</patch>
</preview_patch_application>
```

### Fuzzy Matching Preview
```xml
<preview_patch_application>
<path>src/formatted.js</path>
<patch>--- a/src/formatted.js
+++ b/src/formatted.js
@@ -1,2 +1,3 @@
 const config = {
   debug: false
+  version: '1.0.0'
 };
</patch>
</preview_patch_application>
```

## Output Format

### Success Response
```xml
<tool_response tool_name="preview_patch_application">
  <notes>
    Patch preview for src/config.js
    ✓ Patch can be applied successfully
    Lines to be added: 1
    Lines to be removed: 1
  </notes>
  <result status="SUCCESS" />
  <preview_content>const API_URL = 'https://api.production.com';
const DEBUG = false;
const VERSION = '1.0.0';
</preview_content>
</tool_response>
```

### Failure Response
```xml
<tool_response tool_name="preview_patch_application">
  <notes>
    Patch preview for src/config.js
    ✗ Patch cannot be applied
    Error: Line 'const API_URL = 'http://localhost:3000';' not found
  </notes>
  <result status="FAILED" />
</tool_response>
```

### Fuzzy Matching Response
```xml
<tool_response tool_name="preview_patch_application">
  <notes>
    Patch preview for src/config.js
    ✓ Patch can be applied successfully
    ⚠ Requires fuzzy matching: WhitespaceNormalizationStrategy
    Lines to be added: 1
    Lines to be removed: 0
  </notes>
  <result status="SUCCESS" />
  <preview_content>const API_URL = 'https://api.production.com';
const DEBUG = false;
const VERSION = '1.0.0';
</preview_content>
</tool_response>
```

## Error Handling
- Returns error if file doesn't exist
- Returns error if patch format is invalid
- Returns error if path is outside working directory
- Provides detailed conflict information for failed applications
- Handles malformed patches gracefully

## Validation Features
- **Patch Format Validation**: Ensures unified diff format correctness
- **File Existence Check**: Verifies target file exists
- **Content Matching**: Tests if patch lines match file content
- **Hunk Validation**: Validates each patch hunk individually
- **Context Verification**: Ensures context lines match surrounding code

## Integration with Other Tools
- Often used before `apply_code_patch` for validation
- Can preview patches from `generate_code_patch`
- Commonly preceded by `read_file` to understand current state
- May be used with `search_files` to identify target files

## Advanced Examples

### Multiple Hunk Preview
```xml
<preview_patch_application>
<path>package.json</path>
<patch>--- a/package.json
+++ b/package.json
@@ -2,3 +2,3 @@
   "name": "my-project",
-  "version": "1.0.0",
+  "version": "2.0.0",
   "description": "My project",
@@ -8,3 +8,4 @@
   "scripts": {
     "start": "node server.js",
     "test": "jest"
+    "build": "webpack"
   }
</patch>
</preview_patch_application>
```

### Large File Preview
```xml
<preview_patch_application>
<path>src/large-component.jsx</path>
<patch>--- a/src/large-component.jsx
+++ b/src/large-component.jsx
@@ -45,6 +45,7 @@
   const [data, setData] = useState(null);
   const [loading, setLoading] = useState(false);
+  const [error, setError] = useState(null);
   
   useEffect(() => {
     fetchData();
</patch>
</preview_patch_application>
```

### Configuration File Preview
```xml
<preview_patch_application>
<path>config/database.yml</path>
<patch>--- a/config/database.yml
+++ b/config/database.yml
@@ -1,4 +1,4 @@
 production:
-  host: localhost
+  host: db.production.com
   port: 5432
   database: myapp_production
</patch>
</preview_patch_application>
```

## Security Considerations
- Working directory constraints are enforced
- Path traversal attacks are prevented
- No actual file modifications are performed
- File permissions are respected for read access
- Sensitive information in patches is handled securely

## Performance Considerations
- Efficient preview processing for large files
- Memory-efficient content simulation
- Fast conflict detection algorithms
- Reasonable limits on patch complexity
- Optimized fuzzy matching evaluation

## Debugging Support
The tool helps debug patch application issues by:
- Showing exact line matching failures
- Identifying context mismatches
- Revealing formatting differences
- Highlighting fuzzy matching requirements
- Providing detailed error messages

## Verification Methods
- **Test**: Automated tests verify preview accuracy
- **Analysis**: Static analysis of preview logic
- **Demonstration**: Live demonstration of preview functionality
- **Inspection**: Manual review of preview results

## Related Tools
- `apply_code_patch` - Apply patches after preview validation
- `generate_code_patch` - Create patches for preview
- `read_file` - Examine current file content
- `write_to_file` - Alternative for complete file replacement

## Workflow Integration
Typical workflow using this tool:
1. `read_file` - Examine current content
2. `generate_code_patch` - Create desired patch (optional)
3. `preview_patch_application` - Validate patch safety
4. `apply_code_patch` - Apply patch if preview is satisfactory

## Common Use Cases
- **Code Review**: Preview patches before applying in production
- **Patch Validation**: Ensure patches work with current codebase
- **Conflict Resolution**: Identify and resolve patch conflicts
- **Change Verification**: Verify patches produce expected results
- **Safety Checks**: Ensure patches don't break existing functionality
